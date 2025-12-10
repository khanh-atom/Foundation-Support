#!/usr/bin/env node

import fs from "fs/promises";
import path from "path";
import { exec } from "child_process";
import { promisify } from "util";

const execAsync = promisify(exec);

const log = (msg) => console.log(msg);

const escapeRegex = (value) => value.replace(/([.+^${}()|[\]\\])/g, "\\$1");

const globToRegex = (pattern) => {
  let regex = "";
  for (let i = 0; i < pattern.length; i += 1) {
    const ch = pattern[i];
    const next = pattern[i + 1];

    if (ch === "*" && next === "*") {
      regex += ".*";
      i += 1; // skip the second *
    } else if (ch === "*") {
      regex += "[^/]*";
    } else if (ch === "?") {
      regex += "[^/]";
    } else {
      regex += escapeRegex(ch);
    }
  }
  return regex;
};

const gitignorePatternToRegex = (pattern) => {
  const negated = pattern.startsWith("!");
  const raw = negated ? pattern.slice(1) : pattern;

  const anchored = raw.startsWith("/");
  const dirOnly = raw.endsWith("/");
  const trimmed = raw.replace(/^\/+/, "").replace(/\/+$/, "");

  const body = globToRegex(trimmed);
  const prefix = anchored ? "^" : "^(?:.*\\/)?";
  const suffix = dirOnly ? "(?:\\/.*)?$" : "(?:\\/)?$";

  return { regex: new RegExp(prefix + body + suffix), negated };
};

const pathExists = async (target) => {
  try {
    await fs.access(target);
    return true;
  } catch {
    return false;
  }
};

const loadGitignore = async (rootDir) => {
  const gitignorePath = path.join(rootDir, ".gitignore");
  if (!(await pathExists(gitignorePath))) {
    return [];
  }

  const raw = await fs.readFile(gitignorePath, "utf8");
  return raw
    .split(/\r?\n/)
    .map((line) => line.trim())
    .filter((line) => line && !line.startsWith("#"))
    .map(gitignorePatternToRegex);
};

const shouldIgnore = (relPath, rules) => {
  let ignored = false;
  for (const rule of rules) {
    if (rule.regex.test(relPath)) {
      ignored = !rule.negated;
    }
  }
  return ignored;
};

const ensureRootGitRepo = async () => {
  const gitDir = path.join(process.cwd(), ".git");
  const hasGit = await pathExists(gitDir);

  if (!hasGit) {
    log("⚙️  Initializing Git repo in root directory");
    await execAsync("git init");
  }

  // Best effort to stage .gitmodules if present (mirrors original script behavior)
  await execAsync("git add .gitmodules").catch(() => {});
};

const findGitDirs = async (rootDir, ignoreRules) => {
  const results = [];

  const walk = async (dir) => {
    const entries = await fs.readdir(dir, { withFileTypes: true });
    for (const entry of entries) {
      const fullPath = path.join(dir, entry.name);
      const relEntryPath = path.relative(rootDir, fullPath) || ".";

      if (entry.isDirectory()) {
        if (entry.name === ".git") {
          results.push(fullPath);
          // Do not descend into a .git directory
          continue;
        }

        if (shouldIgnore(relEntryPath, ignoreRules)) {
          continue;
        }

        // Skip heavy dependency trees
        if (entry.name === "node_modules") {
          continue;
        }

        await walk(fullPath);
      }
    }
  };

  await walk(rootDir);
  return results;
};

const getRemoteUrl = async (repoDir) => {
  try {
    const { stdout } = await execAsync(`git -C "${repoDir}" config --get remote.origin.url`);
    return stdout.trim();
  } catch {
    return "";
  }
};

const addSubmodule = async (url, relPath) => {
  try {
    await execAsync(`git submodule add "${url}" "${relPath}"`);
  } catch (err) {
    log(`⚠️  Failed to add submodule for ${relPath}: ${err.message}`);
  }
};

const main = async () => {
  const rootDir = process.cwd();
  await ensureRootGitRepo();

  const ignoreRules = await loadGitignore(rootDir);

  log("🔍 Scanning for Git submodules...");
  const gitDirs = await findGitDirs(rootDir, ignoreRules);

  for (const gitDir of gitDirs) {
    const repoDir = path.dirname(gitDir);
    const relPath = path.relative(rootDir, repoDir) || ".";

    // Skip the root repository itself
    if (relPath === ".") {
      continue;
    }

    log(`🚀 Processing ${relPath}...`);

    const url = await getRemoteUrl(repoDir);
    if (url) {
      log(`➕ Adding submodule: ${relPath} -> ${url}`);
      await addSubmodule(url, relPath);
    } else {
      log(`⚠️  No remote found in ${relPath}. Skipping...`);
    }
  }

  log("");
  log("✅ Done adding submodules.");
};

main().catch((err) => {
  console.error("Unexpected error:", err);
  process.exit(1);
});

