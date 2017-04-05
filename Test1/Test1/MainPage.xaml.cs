using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
//using System.Windows.Media;
using System.Threading.Tasks;
using Windows.UI;
using System.Xml.Linq;
using Windows.UI.Popups;
using System.Text;
//using System.Xaml;
using Windows.Data.Xml.Dom;
using Windows.UI.Xaml.Media;


// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Test1
{
    public delegate void UI_Interface();
    
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        Parallax28340Device RFID = new Parallax28340Device();
        //Unlocked unlock = new Unlocked();
        
        MediaElement Mplayer = new MediaElement();
        FileStream wavFile = null;
        public MainPage()
        {
            InitializeComponent();

            RFID.Init(DisplayRFID, DisplayStatus);

            try
            {
                wavFile = new FileStream("./Sounds/Beep.wav",
                                      FileMode.Open,
                                      FileAccess.Read);

            }
            catch (Exception e)
            {
                DisplayErrorMsg(e.Message);
                wavFile = null;
            }
        }

        public void DisplayErrorMsg(string ErrorMsg)
        {
            Task.Run(() =>
            {
                var dialog = new MessageDialog("Error");
                //await dialog.ShowAsync();
                //MessageBox.Show(ErrorMsg);
            });
        }

        public void DisplayRFID(string RFID)
        {
            Task.Run(() =>
            {
                XElement root = XElement.Load("Valid.xml");   //Load XML file 
                IEnumerable<string> RFIDTags = from tag in root.Elements("RFID_Tag")    //Linq to XML to find all tag #'s
                                               where (string)tag.Attribute("ID") == RFID
                                               select (string)tag.Element("Name").Attribute("Person").Value + "\n" +
                                               (string)tag.Element("CC").Attribute("Num").Value + "\n$" + // returns the name, CC, and bill associated with tags
                                               (string)tag.Element("Bill").Attribute("Total").Value;
                foreach (string tag in RFIDTags)
                { //Loop to check Tags
                    PlaySound(); // Beep

                    string[] info = tag.Split('\n'); //parses returned information
                    if (info[0] == "" && info[1] == "" && info[2] == "$") //checks to see if unidentified tag
                    {
                        ///Window1 CC = new Window1();
                        ///var dialogResult = CC.ShowDialog(); //creates an instance of the swipe card message
                        //StreamReader file = new StreamReader("temp.txt"); //loads the information from CC form
                        string filename = "temp.txt";
                        byte[] byteArray = Encoding.UTF8.GetBytes(filename);
                        MemoryStream stream = new MemoryStream(byteArray);
                        StreamReader file = new StreamReader(stream);
                        StringBuilder data = new StringBuilder();
                        data.Append(file.ReadLine()); //read in last name
                        data.Append(", ");            //make it look perty
                        data.Append(file.ReadLine()); //read in first name

                        /* * * * * * * * * * * * * * * * *
                        *   WRITE TO THE XML FILE HERE   *
                        * * * * * * * * * * * * * * * * * */
                        XmlDocument doc = new XmlDocument();
                        string filename2 = "Valid.xml";
                        byte[] byteArray2 = Encoding.UTF8.GetBytes(filename2);
                        MemoryStream stream2 = new MemoryStream(byteArray2);
                        doc.LoadXml("Valid.xml");
                        XmlNodeList nodes = doc.SelectNodes("Valid_List/RFID_Tag");
                        
                        foreach (IXmlNode ID in nodes)
                        {
                            if (ID.Attributes[0].NodeValue == RFID)
                            {
                                IXmlNode name = ID.SelectSingleNode("Name");
                                name.Attributes[0].NodeValue = data.ToString();
                                IXmlNode cc = ID.SelectSingleNode("CC");
                                cc.Attributes[1].NodeValue = file.ReadLine();
                                IXmlNode bill = ID.SelectSingleNode("Bill");
                                bill.Attributes[2].NodeValue = "0.00";
                                ///doc.Save("Valid.xml");
                            }
                        }

                        ///file.Close();
                        //var unlooooooked = unlock.ShowDialog(); //unlocks machine
                    }
                    else
                    {
                        //var dialogResult = unlock.ShowDialog(); //unlocks if tag associated
                    }
                    
                }



            });
        }

        public void DisplayStatus(Boolean connected)
        {

            //Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,
            //(UI_Interface)delegate ()
            Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, new Windows.UI.Core.DispatchedHandler(() =>
            {
                if (connected)
                {
                    //textBoxStatus.Background = new SolidColorBrush(Color.FromArgb(0, 0, 255, 0));
                    textBoxStatus.Width = 200;
                }
                else
                {

                    //textBoxStatus.Foreground = new SolidColorBrush(Color.FromArgb(0, 0, 250, 0));
                    //textBoxStatus.Fill = new SolidColorBrush(Color.FromArgb(0, 250, 0, 0));
                    textBoxStatus.Fill = new SolidColorBrush(Color.FromArgb(250, 250, 0, 0));
                    //textBoxStatus.Width = 200;
                    //var dialog = new MessageDialog("Your message here");
                    //dialog.ShowAsync().AsTask();
                }

                

            }
            )).AsTask().Start();
        }

        public void PlaySound()
        {
            if (wavFile != null)
            {
                //Mplayer.Stream = wavFile;
                Mplayer.Source = new Uri(@"Assets\Beep.wav");
                Mplayer.Play();
            }
        }
    
}
}
