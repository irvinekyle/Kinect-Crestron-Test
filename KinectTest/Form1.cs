using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using Microsoft.Research.Kinect.Audio;
using Microsoft.Speech.AudioFormat;
using Microsoft.Speech.Recognition;

using Crestron.ActiveCNX;
using System.Diagnostics;
using System.Threading;

namespace KinectTest
{
    public partial class Form1 : Form
    {
        private ActiveCNX acnxConnection;
        private KinectAudioSource audioSource = new KinectAudioSource();
        private SpeechRecognitionEngine sre = new SpeechRecognitionEngine();
        private Choices transport = new Choices();
        private Recognizer recognizer;
        private Log log = new Log();
        private Dictionary<string, Action> actionMap;

        private const string RecognizerId = "SR_MS_en-US_Kinect_10.0";

        public Form1()
        {
            InitializeComponent();
            for (int i = 3; i < 255; i++)
            {
                this.cmbBoxIPID.Items.Add(string.Format("{0:X2}", i));
            }

            this.acnxConnection = new ActiveCNX();

            acnxConnection.onConnect += new ActiveCNXConnectionEventHandler(acnxConnection_onConnect);
            acnxConnection.onDisconnect += new ActiveCNXConnectionEventHandler(acnxConnection_onDisconnect);
            acnxConnection.onError += new ActiveCNXErrorEventHandler(acnxConnection_onError);
            acnxConnection.onDigital += new ActiveCNXEventHandler(acnxConnection_onDigital);

            //I remembered that you can chain as many methods onto a delegate as you want, meaning we don't need a List of actions anymore.
            //Now instead of initializing several lists within the dictionary, we can just initialize it with a null delegate to start and 
            //then add methods onto it as needed. Should be an easier syntax than dealing with the lists...
            Action del = null;

            audioSource.FeatureMode = true;
            audioSource.AutomaticGainControl = false;
            audioSource.SystemMode = SystemMode.OptibeamArrayOnly;

            RecognizerInfo ri = SpeechRecognitionEngine.InstalledRecognizers().Where(r => r.Id == RecognizerId).FirstOrDefault();
            if (ri == null)
            {
                MessageBox.Show("Could not find speech recognizer: {0}.", RecognizerId);
                return;
            }

            this.sre = new SpeechRecognitionEngine(ri.Id);
            this.transport = new Choices(
                "play",
                "stop",
                "next channel",
                "previous channel",
                "cnn",
                "fox");

            var gb = new GrammarBuilder();
            gb.Culture = ri.Culture;
            gb.Append(transport);

            var g = new Grammar(gb);

            sre.LoadGrammar(g);

            this.actionMap = new Dictionary<string, Action>()
            {
                {"play",del},
                {"stop",del},
                {"pause",del},
            };

            actionMap["play"] += () =>
                {
                    this.log.LogEvent("\nPlay command recognized");
                    this.acnxConnection.SendDigital(0, 1, true);
                    this.acnxConnection.SendDigital(0, 1, false);
                };

            actionMap["stop"] += () =>
                {
                    this.log.LogEvent("\nStop command recognized");
                    this.acnxConnection.SendDigital(0, 2, true);
                    this.acnxConnection.SendDigital(0, 2, false);
                };

            actionMap["pause"] += () =>
                {
                    this.log.LogEvent("\nPause command recognized");
                    this.acnxConnection.SendDigital(0, 3, true);
                    this.acnxConnection.SendDigital(0, 3, false);
                };

            //We could also do something like this, if we wanted a way to add actions at runtime; something like this 
            //would let us add new actions to a given command while the form was running, which might be cool
            AddActionToMap("play", () => this.acnxConnection.SendDigital(1, 0, false));

            //To deal with the threading issue, I've moved the speech stuff into it's own class, which is how the ShapeGame
            //sample works. The Recognizer basically just takes an array of strings to set up a speech engine, and has it's 
            //own SpeechRecognized event that we can hook onto - it basically just mirrors the standard SpeechRecognized event. 
            //This means the Kinect stuff can have it's own little MTA threaded playground to fart around in, but the rest of 
            //the code doesn't have to deal with any messy threading issues. 
            recognizer = new Recognizer(this.actionMap.Keys.ToArray());
            this.recognizer.SaidSomething += sre_SpeechRecognized;
        }

        void acnxConnection_onDigital(object sender, ActiveCNXEventArgs e)
        {
            this.log.LogEvent(String.Format("\nDigital join: {0}, Slot: {1}, Value:{2}\r", e.Join.ToString(), e.Slot.ToString(), e.DigitalValue.ToString()));
        }

        private void AddActionToMap(string key, Action del)
        {
            if (actionMap.ContainsKey(key))
            {
                actionMap[key] += del;
            }
            else
                MessageBox.Show(String.Format("ActionMap does not contain command string of type {0}", key));
        }

        void acnxConnection_onError(object sender, ActiveCNXErrorEventArgs e)
        {
        }

        void acnxConnection_onDisconnect(object sender, ActiveCNXConnectionEventArgs e)
        {
        }

        void acnxConnection_onConnect(object sender, ActiveCNXConnectionEventArgs e)
        {
            this.log.LogEvent("\nConnected");
            //this.btnConnect.Enabled = false;
            //this.btnDisconnect.Enabled = true;

            this.acnxConnection.UpdateRequest();
        }

        void sre_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            if (actionMap.ContainsKey(e.Result.Text))
            {
                actionMap[e.Result.Text].InvokeIfNotNull();
            }
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            /*I've hardcoded some values in here, to save on the number of fields we need for the form:
                UserName: admin
                Password: admin
                IpPort: 41794
                UseSSL: true
             */
            if ((this.txtBoxIPAddress != null) && (this.cmbBoxIPID != null))
            {
                this.acnxConnection.Connect(this.txtBoxIPAddress.Text, Convert.ToInt64(this.cmbBoxIPID.SelectedIndex + 3), "", "", 41794, false, 0, 0);
                this.acnxConnection.UpdateRequest();
            }
        }

        private void btnDisconnect_Click(object sender, EventArgs e)
        {
            this.acnxConnection.Disconnect();
        }

        private void btnSendTest_Click(object sender, EventArgs e)
        {
            switch (this.cmbBoxSignalType.Items[this.cmbBoxSignalType.SelectedIndex].ToString())
            {
                case ("Digital"):
                    {
                        if (this.txtBoxJoin.Text != null && Convert.ToInt32(this.txtBoxJoin.Text) > 0)
                        {
                            switch (this.cmbBoxValue.Items[this.cmbBoxValue.SelectedIndex].ToString())
                            {
                                case ("High"):
                                    {
                                        this.acnxConnection.SendDigital(this.cmbBoxSlot.SelectedIndex, Convert.ToInt32(this.txtBoxJoin.Text), true);
                                        break;
                                    }
                                case ("Low"):
                                    {
                                        this.acnxConnection.SendDigital(this.cmbBoxSlot.SelectedIndex, Convert.ToInt32(this.txtBoxJoin.Text), false);
                                        break;
                                    }
                                case ("Pulse (High-Low)"):
                                    {
                                        this.acnxConnection.SendDigital(this.cmbBoxSlot.SelectedIndex, Convert.ToInt32(this.txtBoxJoin.Text), true);
                                        this.acnxConnection.SendDigital(this.cmbBoxSlot.SelectedIndex, Convert.ToInt32(this.txtBoxJoin.Text), false);
                                        break;
                                    }
                                case ("Pulse (Low-High)"):
                                    {
                                        this.acnxConnection.SendDigital(this.cmbBoxSlot.SelectedIndex, Convert.ToInt32(this.txtBoxJoin.Text), false);
                                        this.acnxConnection.SendDigital(this.cmbBoxSlot.SelectedIndex, Convert.ToInt32(this.txtBoxJoin.Text), true);
                                        break;
                                    }
                            }
                        }
                        break;
                    }
                //I'll wire these up later; the original form has some goofiness going on for these signal types, so I can't just copy and paste, and I'm lazy
                case ("Analog"):
                    {
                        break;
                    }
                case ("Serial"):
                    {
                        break;
                    }
            }
        }
       

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (recognizer != null)
                recognizer.Stop();
        }

        private void btnVoice_Click(object sender, EventArgs e)
        {
            switch (this.btnVoice.Text)
            {
                case ("Start Voice"):
                    {

                        Stream s = audioSource.Start();

                        sre.SetInputToAudioStream(s,
                                    new SpeechAudioFormatInfo(
                                        EncodingFormat.Pcm, 16000, 16, 1,
                                        32000, 2, null));

                        Console.WriteLine("Recognizing. Say: 'play', 'stop' or 'pause'. Press ENTER to stop");
                        sre.RecognizeAsync(RecognizeMode.Multiple);

                        this.btnVoice.Text = "Stop Voice";
                        break;
                    }
                case ("Stop Voice"):
                    {
                        sre.RecognizeAsyncStop();
                        audioSource.Stop();
                        Console.WriteLine("Stopping recognizer ...");
                        this.btnVoice.Text = "Start Voice";
                        break;
                    }
            }
        }

    }

    public static class Extensions
    {
        public static void InvokeIfNotNull(this Action del)
        {
            if (del != null)
            {
                del.Invoke();
            }
        }
    }
}
