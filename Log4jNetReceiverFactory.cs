using System;
using System.Reflection;
using System.Windows;
using System.Windows.Media.Imaging;
using JetBrains.Annotations;
using Prism.Modularity;
using Prosa.Log4View.Configuration;
using Prosa.Log4View.Extensibility.Receiver;
using Prosa.Log4View.Frameworks;
using Prosa.Log4View.Interfaces;

namespace Prosa.Log4View.Log4jReceiver
{
    [Module(ModuleName = TypeId), UsedImplicitly]
    public class Log4jNetReceiverFactory : CustomReceiverFactory
    {
        internal const string TypeId = "Prosa.Log4jReceiver";

        public Log4jNetReceiverFactory()
        {
            ReceiverTypeId = TypeId;
            Name = "Log4j Network Receiver";
            HelpKeyword = null;
            ConfigType = new[] { typeof(Log4jNetReceiverConfig) };

            SmallImage = new BitmapImage(GetImageUri("glyph"));
            MediumImage = new BitmapImage(GetImageUri("largeGlyph"));
            BigImage = new BitmapImage(GetImageUri("hugeGlyph"));

            ConfigTemplate = GetDataTemplate();
        }

        private Uri GetImageUri(string imageName)
        {
            var assemblyName = Assembly.GetExecutingAssembly().GetName().FullName;
            return new Uri($"pack://application:,,,/{assemblyName};component/Resources/{imageName}.png", UriKind.Absolute);
        }

        private DataTemplate GetDataTemplate()
        {
            var assemblyName = Assembly.GetExecutingAssembly().GetName().FullName;
            var uri = new Uri($"pack://application:,,,/{assemblyName};component/Resources/ViewTemplate.xaml",
                UriKind.Absolute);

            var dictionary = new ResourceDictionary { Source = uri };
            return (DataTemplate)dictionary["Log4jReceiverTemplate"];
        }

        public override ReceiverConfig CreateReceiverConfig() => new Log4jNetReceiverConfig();

        public override ReceiverConfig CreateReceiverConfig(SourceConfig source, Log4ViewAppenderNode appender) => null;

        public override ILogReceiver CreateReceiver(ReceiverConfig config) => new Log4jNetReceiver(this, (Log4jNetReceiverConfig)config);

        /// <summary>
        /// Returns a custom receiver configuration record.
        /// </summary>
        /// <param name="receiver">The custom receiver configuration.</param>
        /// <param name="edit">Controls, if the configuration dialog is is created to edit an existing receiver (true),
        /// or to create a new receiver (false).</param>
        /// <returns></returns>
        public override ICustomReceiverConfigurator CreateReceiverConfigurator(ReceiverConfig receiver, bool edit) => new Log4jNetReceiverConfigVm((Log4jNetReceiverConfig)receiver);
    }
}
