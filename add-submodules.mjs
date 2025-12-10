#!/usr/bin/env node

import fs from "fs/promises";
import path from "path";
import { exec } from "child_process";
import { promisify } from "util";

const execAsync = promisify(exec);

const log = (msg) => console.log(msg);

const pathExists = async (target) => {
  try {
    await fs.access(target);
    return true;
  } catch {
    return false;
  }
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

const findGitDirs = async () => {
  const results = [];

  const walk = async (dir) => {
    const entries = await fs.readdir(dir, { withFileTypes: true });
    for (const entry of entries) {
      const fullPath = path.join(dir, entry.name);

      if (entry.isDirectory()) {
        if (entry.name === ".git") {
          results.push(fullPath);
          // Do not descend into a .git directory
          continue;
        }

        await walk(fullPath);
      }
    }
  };

  await walk(process.cwd());
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
  await ensureRootGitRepo();

  log("🔍 Scanning for Git submodules...");
  const gitDirs = await findGitDirs();

  for (const gitDir of gitDirs) {
    const repoDir = path.dirname(gitDir);
    const relPath = path.relative(process.cwd(), repoDir) || ".";

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

