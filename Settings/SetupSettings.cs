namespace Martiscoin.Explorer.Settings
{
   public class SetupSettings
   {
      public SetupSettings()
      {
         Title = "Martiscoin Explorer";
      }

      public string Title { get; set; }

      public string Footer { get; set; }

      public string DocumentationUrl { get; set; }
   }
}
