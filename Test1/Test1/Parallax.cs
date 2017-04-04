using System;
using System.Threading.Tasks;
//using System.Timers;
using Windows.UI.Xaml;
using FTD2XX_NET;


namespace Test1
{
    public class Parallax28340Device
    {
        public delegate void delegateOutputRFID(string RFID);
        public delegate void delegateOutputStatus(Boolean connected);

        static delegateOutputRFID OutputRFID;
        static delegateOutputStatus OutputStatus;

        const int MAX_RFID_READ_BUFFER = 12;

        const char CR = '\r';
        const char LF = '\n';
        const char NULL_CHAR = '\0';

        static FTDI RFIDFtdiDevice = new FTDI();

        Task RFIDthrd = new Task(RFID);
        static Boolean RFIDThreadRunning = false;
        static Boolean RFIDExitThread = false;

        static string PrevRFIDdata = "";

        //System.Timers.Timer activityTimer = new System.Timers.Timer();
        static DispatcherTimer activityTimer = new DispatcherTimer();

        public Parallax28340Device()
        {

        }

        public void Init(delegateOutputRFID outputRFID, delegateOutputStatus outputStatus)
        {
            OutputRFID = outputRFID;
            OutputStatus = outputStatus;

            //activityTimer.Elapsed += new ElapsedEventHandler(activityTimerExpired);
            activityTimer.Tick += activityTimerExpired;
            //activityTimer.Interval = 1500; //One and a half seconds
            activityTimer.Interval = new TimeSpan(0, 0, 1);

            //RFIDthrd = new Task(new ThreadStart(RFID));
            RFIDthrd.Start();
            //RFIDthrd.Name = "RFID";
            //RFIDthrd.Start();
        }

        static public void End()
        {
            RFIDExitThread = true;
            while (RFIDThreadRunning)
            {
            }
        }

        static FTDI.FT_STATUS OpenRFIDDevice()
        {

            FTDI.FT_STATUS Status = FTDI.FT_STATUS.FT_OTHER_ERROR;

            Task.Delay(100);

            if (RFIDFtdiDevice.IsOpen)
            {
                Status = RFIDFtdiDevice.Close();
                if (Status != FTDI.FT_STATUS.FT_OK)
                {
                    return Status;
                }
            }

            // One can modify the device descriptor to open by Description
            // Placed here for reference only
            //Status = RFIDFtdiDevice.OpenByDescription("FT232R USB UART RFID Reader");

            //Asume that we are the only FTDI device
            Status = RFIDFtdiDevice.OpenByIndex(0);
            if (Status != FTDI.FT_STATUS.FT_OK)
            {
                return Status;
            }

            Status = RFIDFtdiDevice.SetBaudRate(2400);
            if (Status != FTDI.FT_STATUS.FT_OK)
            {
                return Status;
            }

            Status = RFIDFtdiDevice.SetDataCharacteristics(FTDI.FT_DATA_BITS.FT_BITS_8, FTDI.FT_STOP_BITS.FT_STOP_BITS_1, FTDI.FT_PARITY.FT_PARITY_NONE);
            if (Status != FTDI.FT_STATUS.FT_OK)
            {
                return Status;
            }

            Status = RFIDFtdiDevice.SetFlowControl(FTDI.FT_FLOW_CONTROL.FT_FLOW_NONE, 0x0, 00);
            if (Status != FTDI.FT_STATUS.FT_OK)
            {
                return Status;
            }

            // Enable RFID Device
            Status = RFIDFtdiDevice.SetDTR(true);

            return Status;
        }

        //private void activityTimerExpired(object source, ElapsedEventArgs e)
        private void activityTimerExpired(object source, object e)
        {
            PrevRFIDdata = "";
            activityTimer.Stop();
        }

        static void RFID()
        {

            int RFIDreadDataIndx = 0;
            UInt32 RFIDnumBytesRead = 0;
            uint RFIDRxQueue = 0;

            string RFIDdata = "";

            byte[] RFIDreadData = new byte[MAX_RFID_READ_BUFFER] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            byte[] RFIDreadBuffer = new byte[MAX_RFID_READ_BUFFER] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

            FTDI.FT_STATUS ftStatus = FTDI.FT_STATUS.FT_OTHER_ERROR;

            RFIDThreadRunning = true;

            while (RFIDExitThread == false)
            {
                ftStatus = OpenRFIDDevice();
                if (ftStatus == FTDI.FT_STATUS.FT_OK)
                {
                    OutputStatus(true);
                }
                else
                {
                    OutputStatus(false);
                }

                while (ftStatus == FTDI.FT_STATUS.FT_OK && RFIDExitThread == false)
                {
                    // Poll for data
                    // Sleep so as to keep CPU usage down
                    Task.Delay(10);
                    ftStatus = RFIDFtdiDevice.GetRxBytesAvailable(ref RFIDRxQueue);
                    if (RFIDRxQueue > 0)
                    {
                        // The data does not have to come MAX_RFID_READ_BUFFER bytes at a time and sometimes does not
                        ftStatus = RFIDFtdiDevice.Read(RFIDreadBuffer, RFIDRxQueue, ref RFIDnumBytesRead);
                        for (int i = 0; i < RFIDnumBytesRead; i++)
                        {
                            RFIDreadData[RFIDreadDataIndx] = RFIDreadBuffer[i];
                            if (RFIDreadData[RFIDreadDataIndx] == CR)
                            {
                                if (RFIDreadData[0] == LF)
                                {
                                    RFIDdata = System.Text.Encoding.ASCII.GetString(RFIDreadData, 1, MAX_RFID_READ_BUFFER - 2);

                                    // Avoid duplicate reads
                                    if (PrevRFIDdata != RFIDdata)
                                    {

                                        OutputRFID(RFIDdata);

                                        activityTimer.Stop();

                                        PrevRFIDdata = RFIDdata;
                                        for (int j = 0; j < MAX_RFID_READ_BUFFER; j++)
                                        {
                                            RFIDreadData[j] = 0;
                                        }

                                        activityTimer.Start();
                                    }
                                }

                                RFIDreadDataIndx = 0;
                            }
                            else
                            {
                                // Make sure we are within the read buffer limits
                                if (RFIDreadDataIndx < MAX_RFID_READ_BUFFER)
                                {
                                    RFIDreadDataIndx++;
                                }
                            }
                        }
                    }//if (RFIDRxQueue > 0)
                }// while (ftStatus == FTDI.FT_STATUS.FT_OK && RFIDExitThread == false)
            }// while (RFIDExitThread == false)

            if (RFIDFtdiDevice.IsOpen)
            {
                ftStatus = RFIDFtdiDevice.SetDTR(false);
                ftStatus = RFIDFtdiDevice.Close();
            }

            RFIDThreadRunning = false;
            //Console.WriteLine("RFID Thread Exited  Normally");
            
        }
    }
}