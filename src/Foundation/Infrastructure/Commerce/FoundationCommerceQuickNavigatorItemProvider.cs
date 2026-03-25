using System.Collections.Generic;
using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Core;
using EPiServer.Globalization;
using EPiServer.Shell;
using EPiServer.Web;
using EPiServer.Web.Routing;

namespace Foundation.Infrastructure.Commerce
{
    /// <summary>
    /// Custom Commerce quick navigator item provider for Foundation-Support.
    /// 
    /// This mirrors the platform <see cref="EPiServer.Commerce.Shell.CommerceQuickNavigatorItemProvider"/>
    /// but lives in the site code so it can be customized per project.
    /// 
    /// NOTE: The dictionary key is intentionally not "Commerce" to avoid key collisions
    /// with the built-in provider when both are registered.
    /// </summary>
    public class FoundationCommerceQuickNavigatorItemProvider : IQuickNavigatorItemProvider
    {
        private readonly IContentLoader _contentLoader;
        private readonly EditUrlResolver _editUrlResolver;

        public FoundationCommerceQuickNavigatorItemProvider(
            IContentLoader contentLoader,
            EditUrlResolver editUrlResolver)
        {
            _contentLoader = contentLoader;
            _editUrlResolver = editUrlResolver;
        }

        public IDictionary<string, QuickNavigatorMenuItem> GetMenuItems(ContentReference currentContent)
        {
            var items = new Dictionary<string, QuickNavigatorMenuItem>();

            // Default to Commerce "home" (dashboard).
            var commerceUrl = Paths.ToResource("Commerce", string.Empty);

            // If current request is routed to a catalog entry/content,
            // deep-link into Commerce Catalog and focus that item.
            if (!ContentReference.IsNullOrEmpty(currentContent))
            {
                try
                {
                    var content = _contentLoader.Get<IContent>(currentContent);
                    if (content is CatalogContentBase)
                    {
                        commerceUrl = _editUrlResolver.GetEditViewUrl(
                            currentContent,
                            new EditUrlArguments
                            {
                                ForceEditHost = true,
                                Language = ContentLanguage.PreferredCulture
                            }).ToString();
                    }
                }
                catch
                {
                    // Ignore and fall back to the Commerce dashboard.
                }
            }

            // Use a distinct key to avoid colliding with the platform "Commerce" entry.
            items.Add(
                "FoundationCommerce",
                new QuickNavigatorMenuItem(
                    "Commerce (Custom)",
                    commerceUrl,
                    null,
                    "true",
                    null));

            return items;
        }

        /// <summary>
        /// Slightly after the core CMS quick navigator but before
        /// any very-late custom providers.
        /// </summary>
        public int SortOrder => 15;
    }
}


