using Microsoft.IoT.Lightning.Providers;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Devices;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using AutoGenerateForm.Uwp;
using System.IO;
using Windows.UI.Popups;
using Windows.System;
using Windows.Media;
using System.Collections.Generic;
using System.Text;
using System.Linq;
//using GemBox.Document;

namespace Emmellsoft.IoT.Rpi.AdDaBoard.Demo
{
    /// <summary>
    /// The intention of this class is to show the basic usage of the IAdDaBoard and may serve as a playground for it.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private readonly Func<IAdDaBoard, Task> _currentDemo;

        string[] header = { "Time", "Date", "Vbat", "current", "Cab1", "Cab2", "cabv" };
        List<List<string>> table = new List<List<string>>();

        public MainPage()
        {



            InitializeComponent();


            // Check if Lightning is available and use it
            // The Lightning greatly speedups SPI and GPIO to use it you must enable Direct Memory Mapped driver in Device manager
            // For more information see: https://developer.microsoft.com/en-us/windows/iot/docs/lightningproviders

            if (LightningProvider.IsLightningEnabled)
            {
                Debug.WriteLine("Using Lightning");
                LowLevelDevicesController.DefaultProvider = LightningProvider.GetAggregateProvider();
            }

            //===============================================================
            // Choose demo here!
            // ------------------------------

            //_currentDemo = OutputDemo;
            //_currentDemo = InputDemo;
            //_currentDemo = InputOutputDemo;
            _currentDemo = KnobToScreenDemo; // <-- Requires a monitor connected to the Raspberry Pi.

            //===============================================================

            Task.Run(async () => await Demo()).ConfigureAwait(false);
        }

        private async Task Demo()
        {
            // (Technically the demo will never end, but in a different scenario the IAdDaBoard should be disposed,
            // therefor I chose tho get the AD/DA-board in a using-statement.)

            using (IAdDaBoard adDaBoard = await AdDaBoardFactory.GetAdDaBoard().ConfigureAwait(false))
            {
                await _currentDemo(adDaBoard).ConfigureAwait(false);
            }
        }

        private static async Task OutputDemo(IAdDaBoard adDaBoard)
        {
            int outputLevel = 0;     // This ('outputLevel') variable will from 0 to 100 and back to 0 repeatedly ...
            int outputLevelStep = 5; // ... taking a step of this ('outputLevelStep') value at the time.

            while (true)
            {
                double normalizedOutputLevel = outputLevel / 100.0; // Get a floating point value between 0.0 and 1.0...
                double invertedNormalizedOutputLevel = 1.0 - normalizedOutputLevel; // ...and the "inverted"; between 1.0 and 0.0

                // Set the two outputs one at a time (of course the "SetOutputs" method could also be used here).
                adDaBoard.Output.SetOutput(OutputPin.DAC0, normalizedOutputLevel);
                adDaBoard.Output.SetOutput(OutputPin.DAC1, invertedNormalizedOutputLevel);

                outputLevel += outputLevelStep;
                if ((outputLevel == 0) || (outputLevel == 100))
                {
                    outputLevelStep = -outputLevelStep;
                }

                await Task.Delay(50);
            }
        }

        private static async Task InputDemo(IAdDaBoard adDaBoard)
        {
            const double vRef = 5.0;

            adDaBoard.Input.DataRate = InputDataRate.SampleRate50Sps;
            adDaBoard.Input.AutoSelfCalibrate = true;
            adDaBoard.Input.DetectCurrentSources = InputDetectCurrentSources.Off;
            adDaBoard.Input.Gain = InputGain.Gain1;
            adDaBoard.Input.UseInputBuffer = false;

            // The demo continously reads the value of the 10 kohm potentiometer knob and the photo resistor and
            // writes the values to the Output window in Visual Studio.

            while (true)
            {
                double knobValue = adDaBoard.Input.GetInput(vRef, InputPin.AD0);
                double knobValue1 = adDaBoard.Input.GetInput(vRef, InputPin.AD1);
                double knobValue2 = adDaBoard.Input.GetInput(vRef, InputPin.AD2);
                double knobValue3 = adDaBoard.Input.GetInput(vRef, InputPin.AD3);

                Debug.WriteLine($"Knob: {knobValue:0.0000}, $knob1: {knobValue1:0.0000}, $knob2: {knobValue2:0.0000},$knob3: {knobValue3:0.0000}");

                await Task.Delay(100);



            }
        }

        private static async Task InputOutputDemo(IAdDaBoard adDaBoard)
        {
            adDaBoard.Input.DataRate = InputDataRate.SampleRate50Sps;
            adDaBoard.Input.AutoSelfCalibrate = true;
            adDaBoard.Input.DetectCurrentSources = InputDetectCurrentSources.Off;
            adDaBoard.Input.Gain = InputGain.Gain1;
            adDaBoard.Input.UseInputBuffer = false;

            // The demo continously reads the value of the 10 kohm potentiometer knob and sets the onboard LEDs to its value.
            // Turning the knob to its min/max positions will light up one LED and turn off the other (in the middle both will be shining, but not at full intensity).

            while (true)
            {
                // Get the normalized knob-value between -1.0 and 1.0 (which will actually be between 0.0 and 1.0 due to how the board is constructed):
                double normalizedKnobValue = adDaBoard.Input.GetInput(1.0, InputPin.AD0);
                double normalizedKnobValue1 = adDaBoard.Input.GetInput(1.0, InputPin.AD1);
                double normalizedKnobValue2 = adDaBoard.Input.GetInput(1.0, InputPin.AD2);
                double normalizedKnobValue3 = adDaBoard.Input.GetInput(1.0, InputPin.AD3);

                // ...and the "inverted" value:
                double invertedNormalizedKnobValue = 1.0 - normalizedKnobValue;

                // Set both outputs at the same time:
                adDaBoard.Output.SetOutputs(normalizedKnobValue, invertedNormalizedKnobValue);

                await Task.Delay(10);
            }
        }





        /*
         
        <MediaElement x:Name="beeper"></MediaElement>

        private void AssignBeep()
        {
            Uri beepUri = new Uri("Project;component/beep.mp3", UriKind.RelativeOrAbsolute);
            StreamResourceInfo streamInfo = Application.GetResourceStream(beepUri);
            this.beeper.SetSource(streamInfo.Stream);
            this.beeper.AutoPlay = false;
        }

        private void PlayBeep()
        {
            this.beeper.Position = new TimeSpan(0, 0, 0, 0);
            this.beeper.Volume = 1;
            this.beeper.Play();
        }

        */

        private async Task KnobToScreenDemo(IAdDaBoard adDaBoard)
        {
            adDaBoard.Input.DataRate = InputDataRate.SampleRate50Sps;
            adDaBoard.Input.AutoSelfCalibrate = true;
            adDaBoard.Input.DetectCurrentSources = InputDetectCurrentSources.Off;
            adDaBoard.Input.Gain = InputGain.Gain1;
            adDaBoard.Input.UseInputBuffer = false;

            // The demo continously reads the value of the 10 kohm potentiometer knob and both writes the
            // voltage on the screen and converts the value to an angle, rotating a canvas-drawing on the screen.

            while (true)
            {
                // Get the normalized knob-value between 0.0 and 1.0:
                double normalizedKnobValue = adDaBoard.Input.GetInput(1.0, InputPin.AD0);
                double normalizedKnobValue1 = adDaBoard.Input.GetInput(1.0, InputPin.AD1);
                double normalizedKnobValue2 = adDaBoard.Input.GetInput(1.0, InputPin.AD2);
                double normalizedKnobValue3 = adDaBoard.Input.GetInput(1.0, InputPin.AD3);

                // Convert the normalized knob value to an angle between -130 and 130 degrees.
                double angle = normalizedKnobValue * 260 - 130;
                double angle1 = normalizedKnobValue1 * 260 - 130;
                double angle2 = normalizedKnobValue2 * 260 - 130;
                double angle3 = normalizedKnobValue3 * 260 - 130;

                // Convert the normalized knob value to a voltage as shown in Arduino Code.
                double voltage = normalizedKnobValue * 6.0;
                double voltage1 = (normalizedKnobValue1 * 193) / 10;
                double voltage2 = (normalizedKnobValue2 * 193) / 10;
                double voltage3 = (normalizedKnobValue3 * 193) / 10;

                double temp;
                double barles = 116;
                //char oklevel;
                //char testcable;
                //double cabres = 30;



                temp = normalizedKnobValue1 - normalizedKnobValue;                 //temporarly stored value for calculations
                temp = temp / barles;                    // devide by resistance in milliohms generating current
                temp = ((normalizedKnobValue3 - normalizedKnobValue2) * 10) / temp; // getting all in value of 0.1 mOhm with resolution of 0.2 mOhm



                //Write data to the file

                /*
                //a = time of measurement
                for (int a = 1; a < 60; a++)
                {
                    await Windows.Storage.FileIO.WriteTextAsync(data, voltage.ToString());
                    await Windows.Storage.FileIO.WriteTextAsync(data, voltage1.ToString());
                    await Windows.Storage.FileIO.WriteTextAsync(data, voltage2.ToString());
                    await Windows.Storage.FileIO.WriteTextAsync(data, voltage3.ToString());
                    await Windows.Storage.FileIO.WriteTextAsync(data, "\r\nand");

                }
                */


                //char testmode;
                //uint barles;
                //char oklevel;


                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                    CoreDispatcherPriority.Normal,
                    () =>
                    {
                        // Write the voltage.
                        KnobValue.Text = voltage.ToString("0.000") + " V";
                        KnobValue1.Text = voltage1.ToString("0.000") + " V";
                        KnobValue2.Text = voltage2.ToString("0.000") + " V";
                        KnobValue3.Text = voltage3.ToString("0.000") + " V";


                        // Rotate the canvas drawing an arrow.
                        //ArrowCanvas.RenderTransform = new RotateTransform { CenterX = 0.5, CenterY = 0.5, Angle = angle };
                    });

                await Task.Delay(100);
            }
        }

        private void KnobValue_SelectionChanged(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {

        }


        private void KnobValue1_SelectionChanged(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {

        }

        private void KnobValue2_SelectionChanged(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {

        }
        private void KnobValue3_SelectionChanged(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {

        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void TextBlock_SelectionChanged(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {

        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private async void Button_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {







            int numcab = Convert.ToInt32(combobox1.ToString());
            bool num_chk = int.TryParse(combobox1.ToString(), out numcab);

            if (num_chk)
            {
                numcab += 0;
            }
            else
            {
                var messagedialog = new MessageDialog("Please, enter the number of cables");

                await messagedialog.ShowAsync();
            }


            /////


            int mestime = Convert.ToInt32(combobox2.ToString());
            bool mestime_chk = int.TryParse(combobox2.ToString(), out mestime);

            if (mestime_chk)
            {
                mestime += 0;
            }
            else
            {
                var messagedialog = new MessageDialog("Please, Enter the time of measurement");
                await messagedialog.ShowAsync();
            }

            /////

            int maxres = Convert.ToInt32(combobox3.ToString());
            bool maxres_chk = int.TryParse(combobox3.ToString(), out maxres);

            if (maxres_chk)
            {
                maxres += 0;
            }
            else
            {
                var messagedialog = new MessageDialog("enter the res num");
                await messagedialog.ShowAsync();
            }

            //Create the text file to hold the data
            Windows.Storage.StorageFolder storageFolder =
                Windows.Storage.ApplicationData.Current.LocalFolder;
            Windows.Storage.StorageFile data =
                await storageFolder.CreateFileAsync("data.txt",
                    Windows.Storage.CreationCollisionOption.ReplaceExisting);


            start:

            // all measurements calculations 
            //converting the values to string so we can type it 
            string KnobValue00 = KnobValue.Text;
            string KnobValue11 = KnobValue1.Text;
            string KnobValue22 = KnobValue2.Text;
            string KnobValue33 = KnobValue3.Text;

            //defining the original voltages
            Int32 KnobValueV1 = Convert.ToInt32(KnobValue00);
            Int32 KnobValueV2 = Convert.ToInt32(KnobValue11);
            Int32 KnobValueV3 = Convert.ToInt32(KnobValue22);
            Int32 KnobValueV4 = Convert.ToInt32(KnobValue33);

            //measuring the voltage drop
            Int32 voltagedrop12 = (KnobValueV1 + KnobValueV2) / 2; //Ballast voltage
            Int32 voltagedrop34 = (KnobValueV3 + KnobValueV4) / 2; //Cable voltage
            Int32 voltagedrop14 = (KnobValueV1 + KnobValueV4) / 2; //Battery voltage

            Int32 BattVoltage = voltagedrop14; //printed value of battery voltage

            //measuring the current
            Int32 ballastRes = 100; //MOhm
            Int32 Ic = voltagedrop12 * ballastRes;          //Ic = the current of the circle fixed

            //measuring the required cable ressistance R = I/V
            Int32 cableRes = Ic / voltagedrop34;

            //////////////////////////////////////////////////////////////
            // the show msgs cinditions

            if (BattVoltage < 9)
            {
                var messagedialog = new MessageDialog("The Battery Is low, Please charge it");
                await messagedialog.ShowAsync();
            }

            if (BattVoltage < 9)
            {
                var messagedialog = new MessageDialog("The Battery Is low, Please charge it");
                await messagedialog.ShowAsync();
            }



            for (int i = 0; i < header.Length; i++)
            {
                table[i].Insert(0, header[i]);
            }


            for (int j = 0; j <= mestime; j++)
            {
                //  string time = DateTime.Now.Hour.ToString() +":"+ DateTime.Now.Minute.ToString() + DateTime.Now.Second.ToString();
                string date = DateTime.Today.ToString();
                //  table[0].Add(date);

                table[1].Add(BattVoltage.ToString());
                table[2].Add(Ic.ToString());
                table[3].Add(cableRes.ToString());
                //table[4].Add(CabRes2);

            };

            //// Set license key to use GemBox.Document in Free mode.
            //ComponentInfo.SetLicense("FREE-LIMITED-KEY");

            //// Create a new empty document
            //var document = new DocumentModel();

            //// Add document content.
            //document.Sections.Add(
            //    new Section(document,new Paragraph(document, "Hello world!")));

            //// Save the generated document as PDF file.
            //document.Save("Document.pdf");




            using (FileStream fs = File.Create("result.doc"))
            {
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    for (int i = 0; i < table.Count; i++)

                    {

                        for (int j = 0; j < table[i].Count; j++)
                        {
                            sw.Write(table[i][j] + "/t");

                        }
                        sw.WriteLine();
                    }


                }




                //messagedialog.CancelCommandIndex();

                /* beeps sound
                 * System.Media.SystemSounds.Asterisk.Play();

           System.Media.SystemSounds.Beep.Play();

           System.Media.SystemSounds.Exclamation.Play();

           System.Media.SystemSounds.Hand.Play();

           System.Media.SystemSounds.Question.Play(); 

                */






                ////////////////////////////////////////////
                /* FileStream fs = File.Create("result.txt"); //Creates result.txt
                 List<double> scoreArray = new List<double>();
                 */


                //get the number of cables from the first combobox1

                //string numcables = combobox1.SelectedValue.ToString();
                //Int32 numcab = int.Parse(combobox1.SelectedItem.ToString());

                //if (numcab != null)
                //{
                //    var messagedialogx = new MessageDialog("Please insert the Number of Cables");
                //    //await messagedialog.ShowAsync();
                //}

                //// *** get the time of measurement from the second combobox2 ***

                ////string measutime = combobox2.SelectedValue.ToString();
                //Int32 mestime = int.Parse(combobox2.SelectedItem.ToString());

                //// *** get the value of the max Ressistance from the third combobox3 ***

                ////string maxRes = combobox2.SelectedValue.ToString();
                //Int32 maxRes = int.Parse(combobox2.SelectedItem.ToString());














                /*
                 saveFileDialog saveFileDialog = new SaveFileDialog();
                if (saveFileDialog.ShowDialog() == true)
                    File.WriteAllText(saveFileDialog.FileName, txtEditor.Text);
                */

                /*
                for (int i = 0; i < textBoxes.Length; i++)
                {
                    scoreArray.Add(Convert.ToDouble(textBoxes[i].Text));
                }
                StreamWriter sw = new StreamWriter("result.txt", true);

                */


                //fs.Close(); //Closes file stream


                /*
                   Stream myStream;
                   SaveFileDialog saveFileDialog1 = new SaveFileDialog();

                   saveFileDialog1.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
                   saveFileDialog1.FilterIndex = 2;
                   saveFileDialog1.RestoreDirectory = true;

                   if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                   {
                       if ((myStream = saveFileDialog1.OpenFile()) != null)
                       {
                           // Code to write the stream goes here.
                           myStream.Close();
                       }
                   }

                    */


                //MessageBox.Show(comboBox1lectedItem.ToString());




                /*
                //////////////////////////////////////////
                // Displays a SaveFileDialog so the user can save the Image  
                // assigned to Button2.  
                SaveFileDialog saveFileDialog1 = new SaveFileDialog();
                saveFileDialog1.Filter = "Text File|*.txt|PDF File|*.pdf;
                saveFileDialog1.Title = "Save a File";
                saveFileDialog1.ShowDialog();

                // If the file name is not an empty string open it for saving.  
                if (saveFileDialog1.FileName != "")
                {
                    // Saves the Image via a FileStream created by the OpenFile method.  
                    System.IO.FileStream fs =
                       (System.IO.FileStream)saveFileDialog1.OpenFile();
                    // Saves the Image in the appropriate ImageFormat based upon the  
                    // File type selected in the dialog box.  
                    // NOTE that the FilterIndex property is one-based.  
                    switch (saveFileDialog1.FilterIndex)
                    {
                        case 1:
                            this.button.Image.Save(fs,
                               System.Drawing.Imaging.ImageFormat.Jpeg);
                            break;

                        case 2:
                            this.button2.Image.Save(fs,
                               System.Drawing.Imaging.ImageFormat.Bmp);
                            break;

                        case 3:
                            this.button2.Image.Save(fs,
                               System.Drawing.Imaging.ImageFormat.Gif);
                            break;
                    }

                    fs.Close();
                }


                */

                ////////////////////////////////////////
                //Create the text file to hold the data

                /*   Windows.Storage.StorageFolder storageFolder =
                       Windows.Storage.ApplicationData.Current.LocalFolder;
                   Windows.Storage.StorageFile data =
                       await storageFolder.CreateFileAsync("data.txt",
                           Windows.Storage.CreationCollisionOption.ReplaceExisting);

                   /* making buffer for the data to save it to the file
                   var buffer = Windows.Security.Cryptography.CryptographicBuffer.ConvertStringToBinary(
                    "What fools these mortals be", Windows.Security.Cryptography.BinaryStringEncoding.Utf8);
                   await Windows.Storage.FileIO.WriteBufferAsync(data, buffer);
                   */







                // Plays the sound associated with the Beep system event.
                /*
                          public class SystemSound { }

                        public static SystemSound Beep { get; }
                        SystemSound.Beep.Play();

                    */







                // File.WriteAllLines(data, contentsAsStringArray);
                // Example #1: Write an array of strings to a file.
                // Create a string array that consists of three lines. //just set a timer of measuring time then apply it to the writing file array
                /* {Time = 100}
                 string[] lines = { "First line", "Second line", "Third line" };
                 end if
                     */
                // WriteAllLines creates a file, writes a collection of strings to the file,
                // and then closes the file.  You do NOT need to call Flush() or Close().
                // System.IO.File.WriteAllLines(@"C:\User Folders\ Documents\data.txt", linesvalue);

                ////////////////////////////////////////
                //getting the data from the user to use it after

                //Checking and return for num of cable measurements

                if (numcab > 1)
                {
                    var messagedialog = new MessageDialog("please test the second cable");
                    await messagedialog.ShowAsync();
                    goto start;
                }

                else if (numcab > 2)
                {
                    var messagedialog = new MessageDialog("please test the third cable");
                    await messagedialog.ShowAsync();
                    goto start;
                }
                else if (numcab > 3)
                {
                    var messagedialog = new MessageDialog("please test the fourth cable");
                    await messagedialog.ShowAsync();
                    goto start;
                }









            }


        }

        private void ComboBox_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {
            // get the number of cables down here
            //ComboBox.GetIsSelectionActive;

            // string numcables = comboBox.SelectedValue.ToString();
            //Int32 combobox1 = GetValue(combobox1);
            //GetValue.combobox;





        }

        private void ComboBox_SelectionChanged_2(object sender, SelectionChangedEventArgs e)
        {
            //get the time of measurement down here

        }

        private void ComboBox_SelectionChanged_3(object sender, SelectionChangedEventArgs e)
        {
            //get the maximum ressistance down here 
            /*
            void GetValue(ComboBox combobox3)
            {
                throw new NotImplementedException();
            }

            GetValue(combobox3);*/
        }
    }
}






// save button

/* {


     // creating Excel Application  
     Microsoft.Office.Interop.Excel._Application app = new Microsoft.Office.Interop.Excel.Application();
     // creating new WorkBook within Excel application  
     Microsoft.Office.Interop.Excel._Workbook workbook = app.Workbooks.Add(Type.Missing);
     // creating new Excelsheet in workbook  
     Microsoft.Office.Interop.Excel._Worksheet worksheet = null;
     // see the excel sheet behind the program  
     app.Visible = true;
     // get the reference of first sheet. By default its name is Sheet1.  
     // store its reference to worksheet  
     worksheet = workbook.Sheets["Sheet1"];
     worksheet = workbook.ActiveSheet;
     // changing the name of active sheet  
     worksheet.Name = "Exported from gridview";
     // storing header part in Excel  
     for (int i = 1; i < dataGridView1.Columns.Count + 1; i++)
     {
         worksheet.Cells[1, i] = dataGridView1.Columns[i - 1].HeaderText;
     }
     // storing Each row and column value to excel sheet  
     for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
     {
         for (int j = 0; j < dataGridView1.Columns.Count; j++)
         {
             worksheet.Cells[i + 2, j + 1] = dataGridView1.Rows[i].Cells[j].Value.ToString();
         }
     }
     // save the application  
     workbook.SaveAs("c:\\output.xls", Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Microsoft.Office.Interop.Excel.XlSaveAsAccessMode.xlExclusive, Type.Missing, Type.Missing, Type.Missing, Type.Missing);
     // Exit from the application  
     app.Quit();
     */

/*
//save button for the file 
var savePicker = new Windows.Storage.Pickers.FileSavePicker();
savePicker.SuggestedStartLocation =
    Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
// Dropdown of file types the user can save the file as
savePicker.FileTypeChoices.Add("Plain Text", new List<string>() { ".pdf" });
// Default file name if the user does not type one in or select a file to replace
savePicker.SuggestedFileName = "New Document";


}*/

/*private void TextBlock_SelectionChanged_1(object sender, Windows.UI.Xaml.RoutedEventArgs e)
{

}*/

 
 
 

 // Shutdowns the device immediately:
 //  ShutdownManager.BeginShutdown(ShutdownKind.Shutdown, TimeSpan.FromSeconds(0));


// Restarts the device within 5 seconds:
//  ShutdownManager.BeginShutdown(ShutdownKind.Restart, TimeSpan.FromSeconds(5));


// guide text 
// var messagedialog1 = new MessageDialog("help guide text");
// await messagedialog1.ShowAsync();
//messagedialog.CancelCommandIndex();