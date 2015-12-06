using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;
using WPFLife.Engine;

namespace WPFLife
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, ILifeWindow
    {
        private readonly WPFLife.Engine.Engine engine;
        private readonly Dictionary<string, string> patternsDictionary = new Dictionary<string, string>();

        public MainWindow()
        {
            InitializeComponent();

            engine = new WPFLife.Engine.Engine(this, (int)ImageControl.Width, (int)ImageControl.Height);
            ImageControl.Source = engine.bitmap;

            cbDelay.SelectedIndex = 1;  // 300 milliseconds
            ddlPatterns_populate();
        }

        public void SetGenerationMessage(int generationNumber)
        {
            tbGenerationMessage.Text = "Generation number " + generationNumber.ToString();
        }

        public void SetRules(bool rulesParam)
        {
            rbStandardRules.IsChecked = !rulesParam;
            rb34Rules.IsChecked = rulesParam;
        }

        public void SetAutoStop(bool autoStopParam)
        {
            cbAutoStop.IsChecked = autoStopParam;
        }

        private void ddlPatterns_populate()
        {
            patternsDictionary.Clear();
            cbPatternNames.Items.Clear();

            var cbiFirst = new ComboBoxItem();

            cbiFirst.Content = "Select a pattern...";
            cbPatternNames.Items.Add(cbiFirst);

            var xmlDoc = new XmlDocument();

            xmlDoc.Load("Patterns/index.xml");

            var nodeList = xmlDoc.SelectNodes("/patterns/pattern");

            foreach (XmlNode patternNode in nodeList)
            {
                var name = patternNode.SelectSingleNode("name").InnerText;
                var filename = patternNode.SelectSingleNode("filename").InnerText;

                patternsDictionary.Add(name, filename);

                var cbi = new ComboBoxItem();

                cbi.Content = name;
                cbPatternNames.Items.Add(cbi);  // Or just ...Add(name); ?
            }

            cbPatternNames.SelectedIndex = 0;
        }

        private void ClearCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            engine.btnClear_onClick();
        }

        private void RandomCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            engine.btnRandom_onClick();
        }

        private void RememberCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            engine.btnRemember_onClick();
        }

        private void RecallCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            engine.btnRecall_onClick();
        }

        private void StepCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            engine.btnStep_onClick();
        }

        private void GoCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            engine.btnGo_onClick();
        }

        private void StopCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            engine.btnStop_onClick();
        }

        private void CloseCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void btnLoad_Click(object sender, RoutedEventArgs e)
        {
            var cbi = cbPatternNames.SelectedValue as ComboBoxItem;

            if (cbi == null)
            {
                return;
            }

            var name = cbi.Content as string;

            if (string.IsNullOrEmpty(name) || !patternsDictionary.ContainsKey(name))
            {
                return;
            }

            var filename = patternsDictionary[name];
            var xmlDoc = new XmlDocument();

            xmlDoc.Load("Patterns/" + filename);
            engine.LoadPattern(xmlDoc);
        }

        private void ImageControl_MouseUp(object sender, MouseButtonEventArgs e)
        {
            var position = e.GetPosition(ImageControl);

            engine.onCanvasClick((int)position.X, (int)position.Y);
        }

        private void rbRules_Click(object sender, RoutedEventArgs e)
        {
            engine.rbRules_onClick(rb34Rules.IsChecked.HasValue && rb34Rules.IsChecked.Value);
        }

        private void cbDelay_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var cbi = cbDelay.SelectedValue as ComboBoxItem;

            if (cbi == null)
            {
                return;
            }

            var newDelayAsString = cbi.Content as string;
            int newDelay;

            if (int.TryParse(newDelayAsString, out newDelay) && newDelay > 0)
            {
                engine.ddlDelay_onChange(newDelay);
            }
        }

        private void cbAutoStop_Click(object sender, RoutedEventArgs e)
        {
            engine.cbAutoStop_onChange(cbAutoStop.IsChecked.HasValue && cbAutoStop.IsChecked.Value);
        }
    }

    // Custom commands: See http://stackoverflow.com/questions/601393/custom-command-wpf
    // This class is the second class in the file because the MainWindow class must be first.

    public static class Command
    {
        public static readonly RoutedUICommand Clear = new RoutedUICommand("Clear", "Clear", typeof(MainWindow));
        public static readonly RoutedUICommand Random = new RoutedUICommand("Random", "Random", typeof(MainWindow));
        public static readonly RoutedUICommand Remember = new RoutedUICommand("Remember", "Remember", typeof(MainWindow));
        public static readonly RoutedUICommand Recall = new RoutedUICommand("Recall", "Recall", typeof(MainWindow));
        public static readonly RoutedUICommand Step = new RoutedUICommand("Step", "Step", typeof(MainWindow));
        public static readonly RoutedUICommand Go = new RoutedUICommand("Go", "Go", typeof(MainWindow));
    }
}
