using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using Mediachase.BusinessFoundation.Data;
using Mediachase.BusinessFoundation.Data.Meta.Management;
using Mediachase.Commerce.Customers;

namespace Test
{
    [InitializableModule]
    [ModuleDependency(typeof(EPiServer.Commerce.Initialization.InitializationModule))]
    public class TestClassInitialization : IInitializableModule
    {
        public void Initialize(InitializationEngine context)
        {
            using (var scope = DataContext.Current.MetaModel.BeginEdit(MetaClassManagerEditScope.SystemOwner, AccessLevel.System))
            {
                InitializeTestClass();
                InitializeTestBridgeClass();

                scope.SaveChanges();
            }
        }

        public void Uninitialize(InitializationEngine context)
        {

        }

        private static void InitializeTestBridgeClass()
        {
            var manager = DataContext.Current.MetaModel;
            var metaClass = manager.MetaClasses["MyTestBridgeClass"];
            if (metaClass != null)
            {
                return;
            }

            var name = "MyTestBridgeClass";
            manager.CreateBridgeMetaClass(
                "MyTestBridgeClass", "MyTestBridgeClass", name,
                $"cls_MyTestBridgeClass",
                OrganizationEntity.ClassName, OrganizationEntity.ClassName, name, true,
                "MyTestClass", "MyTestClass", name, true
            );
        }

        private static void InitializeTestClass()
        {
            var manager = DataContext.Current.MetaModel;
            var metaClass = manager.MetaClasses["MyTestClass"];
            if (metaClass == null)
            {
                metaClass = manager.CreateMetaClass("MyTestClass", "MyTestClass", "MyTestClass", $"cls_MyTestClass", PrimaryKeyIdValueType.Guid);
                metaClass.AddPermissions();
            }

            using (var builder = new MetaFieldBuilder(metaClass))
            {
                if (!metaClass.Fields.Contains("FileName"))
                {
                    builder.CreateText("FileName", "File Name", true, 256, false, true);
                }

                if (!metaClass.Fields.Contains("ContentType"))
                {
                    builder.CreateText("ContentType", "Content Type", true, 100, false, true);
                }

                if (!metaClass.Fields.Contains("SharepointFileId"))
                {
                    builder.CreateText("SharepointFileId", "Sharepoint File Id", true, 256, false, true);
                }

                if (!metaClass.Fields.Contains("FileUrl"))
                {
                    builder.CreateText("FileUrl", "File URL", true, 512, false, true);
                }

                if (!metaClass.Fields.Contains("FileSize"))
                {
                    builder.CreateInteger("FileSize", "File Size", true, 0, true);
                }

                if (!metaClass.Fields.Contains("KYCDocumentRejectionComment"))
                {
                    builder.CreateText("KYCDocumentRejectionComment", "KYC Document Rejection Comment", true, 50, false, true);
                }

                builder.SaveChanges();
            }
            metaClass.TitleFieldName = "SharepointFileId";
        }
    }
}
