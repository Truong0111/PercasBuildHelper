namespace PercasHelper.Editor
{
    public static class Constants
    {
        public const string DefaultProductName = "Percas Game";
        public const string DefaultPackageName = "com.percas.game";
        public const string DefaultAlias = "percas";

        public static class Path
        {
            public const string Builds = "Builds";
            public const string PackageBase = "Assets";

            public const string Assets = "Assets";

            public const string LogoIcon = PackageBase + "/Percas/Assets/percas-logo.png";
            public const string LogoSplash = PackageBase + "/Percas/Assets/percas-splash.png";

            public const string PercasStorage = PackageBase + "/Percas/Assets";
            public const string PercasAssets = Assets + "/Percas";

            public const string GoogleServiceJson = PackageBase + "/google-services.json";

            public const string GoogleServicesXMLPath =
                "Assets/Plugins/Android/FirebaseApp.androidlib/res/values/google-services.xml";

            public const string GoogleServicesCheckerXMLPath = "/Percas/Editor/Validator/google-services-checker.xml";

            public const string KeyStore = PackageBase + "/Keystore/user.keystore";
        }
    }
}