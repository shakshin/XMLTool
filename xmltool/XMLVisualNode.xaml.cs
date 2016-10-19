using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
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
    /// Логика взаимодействия для XMLVisualNode.xaml
    /// </summary>
    public partial class XMLVisualNode : UserControl
    {
        private XElement src;
        private Visual owner;

       

        private void SetupCaption()
        {
            captionContainer.BorderBrush = Brushes.Black;
            captionContainer.Background = Brushes.LightYellow;  
        }

        private void SetupCaptionEx()
        {
            captionEx.Children.Add(new TextBlock()
            {
                Text = src.Name.LocalName,
                FontWeight = FontWeights.Bold
            });
            if (src.HasAttributes)
            {
                captionEx.Children.Add(new TextBlock()
                {
                    Text = "Attributes:"
                });
                foreach (XAttribute atr in src.Attributes())
                    captionEx.Children.Add(new TextBlock()
                    {
                        Text = String.Format("  {0}=\"{1}\"", atr.Name.LocalName, atr.Value)
                    });
            }
            if (!src.HasElements && src.Value != string.Empty)
            {
                captionEx.Children.Add(new TextBlock()
                {
                    Text = "Text:"
                });
                captionEx.Children.Add(new TextBlock()
                {
                    Text = src.Value
                });
            }

         
        }

        public XMLVisualNode(XElement source, Visual owner)
        {
            src = source;
            this.owner = owner;
            InitializeComponent();

            SetupCaption();
            SetupCaptionEx();

            expandContainer.Visibility = Visibility.Collapsed;

            if (src.HasElements)
            {
                expander.Stroke = Brushes.Black;
                expander.Fill = Brushes.Green;
                
                expander.MouseDown += delegate
                {
                    if (expandContainer.Visibility == Visibility.Collapsed)
                    {
                        expandContainer.Visibility = Visibility.Visible;
                        expander.Fill = Brushes.Red;
                        App.Current.Dispatcher.Invoke(delegate
                        {
                            Point p = expander.TransformToVisual(owner).Transform(new Point(0, 0));
                            (owner as ScrollViewer).ScrollToVerticalOffset((p.Y - ((owner as ScrollViewer).RenderSize.Height / 2)) + (owner as ScrollViewer).VerticalOffset);
                        }, DispatcherPriority.ContextIdle);
                    }
                    else
                    {
                        expandContainer.Visibility = Visibility.Collapsed;
                        expander.Fill = Brushes.Green;
                    }
                };
                IEnumerable<XElement> elements = src.Elements();
                int cnt = 0;

                foreach (XElement child in elements)
                {
                    cnt++;

                    StackPanel container = new StackPanel() {
                        Orientation = Orientation.Horizontal
                    };
                    if (cnt == 1 || cnt == elements.Count())
                    {
                        if (elements.Count() > 1)
                        {
                            Grid gr0 = new Grid();
                            gr0.RowDefinitions.Add(new RowDefinition());
                            gr0.RowDefinitions.Add(new RowDefinition());

                            Rectangle r0 = new Rectangle()
                            {
                                Stroke = null
                            };
                            r0.SetValue(Grid.RowProperty, cnt == 1 ? 0 : 1);

                            Rectangle r1 = new Rectangle()
                            {
                                Stroke = Brushes.Black,
                                Width = 1
                            };
                            r1.SetValue(Grid.RowProperty, cnt == 1 ? 1 : 0);

                            gr0.Children.Add(r0);
                            gr0.Children.Add(r1);

                            container.Children.Add(gr0);
                        }
                    } else
                    {
                        container.Children.Add(new Rectangle() {
                            Stroke = Brushes.Black,
                            Width = 1
                        });
                    }

                    container.Children.Add(new Rectangle() {
                        Stroke = Brushes.Black,
                        Height = 1,
                        Width = 20,
                        VerticalAlignment = VerticalAlignment.Center
                    });

                    XMLVisualNode childNode = new XMLVisualNode(child, owner);
                    TransformGroup grp = new TransformGroup();
                    grp.Children.Add(new TranslateTransform() { X = -10 });
                    childNode.RenderTransform = grp;
                    container.Children.Add(childNode);

                    stackItems.Children.Add(container);
                }
            }
            else
            {
                expander.Visibility = Visibility.Collapsed;
            }
        }

        private void captionEx_MouseDown(object sender, MouseButtonEventArgs e)
        {
            IXmlLineInfo info = (IXmlLineInfo)src;
            if (info.HasLineInfo())
            {
                TextEditor text = ((App.Current as App).MainWindow as MainWindow).text;
                text.ScrollTo(info.LineNumber, info.LinePosition);
                text.TextArea.Caret.Line = info.LineNumber;
                text.TextArea.Caret.Column = info.LinePosition;
                DocumentLine line = text.Document.GetLineByOffset(text.CaretOffset);
                text.Select(line.Offset, line.Length);
            }
        }
    }
}
