using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Folding;
using ICSharpCode.AvalonEdit.Search;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml;
using System.Xml.Linq;

namespace xmlview
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Config cfg = Config.LoadFromFile();
        private double fontSize;

        private FoldingManager foldingManager;
        private XmlFoldingStrategy foldingStrategy;

        private System.Timers.Timer activityTimer;
        private double ticks = -1;

        private String path = String.Empty;

        private bool visDrag = false;
        private Point lastPos = new Point(-1000, 0);

        public MainWindow()
        {
            InitializeComponent();

            this.Title = "XML Tool by Sergey V. Shakshin";

            if (cfg.EditorHeight > 0) {
                editorArea.Height = new GridLength(cfg.EditorHeight);
            }

            fontSize = cfg.FontSize;
            SetFontSize();

            SetupEditor();

            string[] args = System.Environment.GetCommandLineArgs();
            if (args.Count() > 1)
            {
                OpenFile(args[1]);
            }

            InitTimer();
        }

        private void InitTimer()
        {
            activityTimer = new System.Timers.Timer(1000);
            activityTimer.Elapsed += delegate {
                if (ticks > 0)
                {
                    ticks--;
                }
                else if (ticks == 0)
                {
                    ticks = -1;
                    App.Current.Dispatcher.Invoke(delegate {
                        RebuildTree();
                        foldingStrategy.UpdateFoldings(foldingManager, text.Document);
                    });

                }
            };
            activityTimer.Start();
        }


        private void OpenFile(string path)
        {
            String content = String.Empty;
            try
            {
                this.path = path;
                this.Title = System.IO.Path.GetFileName(path) + " - XML Tool by Sergey V. Shakshin";
                content = new StreamReader(path).ReadToEnd();
            }
            catch (Exception ex)
            {
                MessageBox.Show(String.Format("Failed to open file:\n{0}", ex.Message), "Eerror", MessageBoxButton.OK);
                this.Close();
            }
            text.Text = content;
            foldingStrategy.UpdateFoldings(foldingManager, text.Document);
            RebuildTree();
            text.IsModified = false;
        }

        private Boolean SaveFile()
        {
            if (path == String.Empty)
            {
                SaveFileDialog dlg = new SaveFileDialog();
                dlg.DefaultExt = ".xml";
                dlg.Filter = "XML files|*.xml|All files|*.*";
                dlg.CheckPathExists = true;
                dlg.OverwritePrompt = true;
                if (dlg.ShowDialog() == true)
                {
                    path = dlg.FileName;
                }
                else
                {
                    return false;
                }
            }
            try
            {
                StreamWriter writer = new StreamWriter(path, false);
                writer.Write(text.Text);
                writer.Close();
                text.IsModified = false;
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(String.Format("Failed to save file:\n{0}", ex.Message), "Eerror", MessageBoxButton.OK);
                return false;
            }
        }

        private void SetupEditor()
        {
            text.ShowLineNumbers = true;

            text.TextArea.DefaultInputHandler.NestedInputHandlers.Add(new SearchInputHandler(text.TextArea));

            foldingManager = FoldingManager.Install(text.TextArea);
            foldingStrategy = new XmlFoldingStrategy();
        }

        private void RebuildTree()
        {
            tree.Items.Clear();
            panelVisuals.Children.Clear();
            String content = text.Text;
                
            try
            {
                XDocument xdoc = XDocument.Parse(content, LoadOptions.SetLineInfo);
                

                foreach (XElement x in xdoc.Elements())
                {
                    AddElement(x, null);
                    panelVisuals.Children.Add(new XMLVisualNode(x, visScroller));
                }
            }
            catch (Exception ex)
            {
                tree.Items.Clear();
                tree.Items.Add(String.Format("Parse error: {0}", ex.Message));

                panelVisuals.Children.Clear();
                panelVisuals.Children.Add(new TextBlock() {
                    FontSize = fontSize,
                    Text = String.Format("Parse error: {0}", ex.Message)
                });
            }
            
        }

        private void SetFontSize()
        {
            tree.FontSize = fontSize;
            tree.InvalidateVisual();

            text.FontSize = fontSize;
            text.InvalidateVisual();

            panelVisuals.SetValue(FontSizeProperty, fontSize);
            panelVisuals.InvalidateVisual();

            cfg.FontSize = fontSize;
            cfg.SaveToFile();
        }

        private StackPanel BuildHeader(XElement element)
        {
            StackPanel header = new StackPanel();
            header.Orientation = Orientation.Horizontal;

            header.Children.Add(new TextBlock()
            {
                Text = "<" + element.Name.LocalName,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.Blue
            });



            foreach (XAttribute a in element.Attributes())
            {
                header.Children.Add(new TextBlock()
                {
                    Text = " " + a.Name.LocalName,
                    Foreground = Brushes.Green
                });
                header.Children.Add(new TextBlock()
                {
                    Text = "=\"" + a.Value + "\"",
                    Foreground = Brushes.Brown
                });

            }

            bool needText = element.Elements().Count() == 0 && element.Value != string.Empty;

            header.Children.Add(new TextBlock()
            {
                Text = needText ? ">" : "/>",
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.Blue
            });

            if (needText)
            {
                header.Children.Add(new TextBlock()
                {
                    Text = element.Value
                });
                header.Children.Add(new TextBlock()
                {
                    Text = "</" + element.Name.LocalName + ">",
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.Blue
                });
            }

            return header;
        }

        private StackPanel BuildToolTip(XElement element)
        {
            StackPanel tip = new StackPanel();
            tip.Orientation = Orientation.Vertical;

            tip.Children.Add(new TextBlock() {
            //    FontWeight = FontWeights.Bold,
                Text = element.ToString()
            });

            


            return tip;
        }

        private void AddElement(XElement element, TreeViewItem parent)
        {
            TreeViewItem newItem = new TreeViewItem();
            newItem.Tag = element;
            newItem.IsExpanded = true;

            newItem.Header = BuildHeader(element);
            //newItem.ToolTip = BuildToolTip(element);

            if (parent == null)
            {

                tree.Items.Add(newItem);

            }
            else
            {
            
                parent.Items.Add(newItem);
            
            }
            
            foreach (XElement x in element.Elements()) AddElement(x, newItem);
            
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                if (e.Key == Key.OemPlus)
                {
                    fontSize++;
                    SetFontSize();
                } else if (e.Key == Key.OemMinus)
                {
                    fontSize--;
                    SetFontSize();
                } else if (e.Key == Key.S)
                {
                    SaveFile();
                }
            } else if (e.Key == Key.Escape) this.Close();
        }

        private void tabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
         //   if (tabs.SelectedItem == tabTree) RebuildTree();
        }

        private void text_KeyUp(object sender, KeyEventArgs e)
        {
            ticks = 1;
        }

        private void GridSplitter_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            cfg.EditorHeight = editorArea.ActualHeight;
            cfg.SaveToFile();
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            SaveFile();
        }

        private void MenuItem_Click_1(object sender, RoutedEventArgs e)
        {
            String tmp = path;
            path = String.Empty;
            SaveFile();
            if (path == String.Empty) path = tmp;
        }

        private void MenuItem_Click_2(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private Boolean Exit()
        {
            if (text.IsModified)
            {
                MessageBoxResult res = MessageBox.Show(this, "XML was modified. Would you like to save it?", "Warning", MessageBoxButton.YesNoCancel);
                if (res == MessageBoxResult.Cancel) return false;
                if (res == MessageBoxResult.No) return true;
                if (res == MessageBoxResult.Yes)
                {
                    if (SaveFile()) return true;
                    return false;
                }
            }
            return true;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!Exit()) e.Cancel = true;
        }

        private void visScroller_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (lastPos.X == -1000) {
                    lastPos = e.GetPosition(visScroller);
                    return;
                }
                Point pos = e.GetPosition(visScroller);
                double xoff = pos.X - lastPos.X;
                double yoff = pos.Y - lastPos.Y;
                lastPos = pos;
                if (xoff != 0)
                    visScroller.ScrollToHorizontalOffset(visScroller.HorizontalOffset - xoff);
                if (yoff != 0)
                    visScroller.ScrollToVerticalOffset(visScroller.VerticalOffset - yoff);
            } else
            {
                lastPos.X = -1000;
            }
        }
        

        private void tree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (tree.SelectedItem == null) return;
            IXmlLineInfo info = (tree.SelectedItem as TreeViewItem).Tag as IXmlLineInfo;
            if (info.HasLineInfo())
            {
                TextEditor text = ((Application.Current as App).MainWindow as MainWindow).text;
                text.ScrollTo(info.LineNumber, info.LinePosition);
                text.TextArea.Caret.Line = info.LineNumber;
                text.TextArea.Caret.Column = info.LinePosition;
                DocumentLine line = text.Document.GetLineByOffset(text.CaretOffset);
                text.Select(line.Offset, line.Length);
            }
        }
    }
}
