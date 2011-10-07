using System;
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

namespace KinectTest
{
    public partial class Form1 : Form
    {
        private ActiveCNX acnxConnection;
        private KinectAudioSource audioSource;
        private SpeechRecognitionEngine sre;
        private Choices transport = new Choices();
        private Log log = new Log();
        private Dictionary<string, List<Action>> actionMap;
        private bool voiceStarted = false;

        private const string RecognizerId = "SR_MS_en-US_Kinect_10.0";

        public Form1()
        {
            InitializeComponent();
            for (int i = 3; i < 255; i++)
            {
                this.cmbBoxIPID.Items.Add(string.Format("{0:X2}", i));
            }

            this.acnxConnection = new ActiveCNX();
            this.audioSource = new KinectAudioSource();

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

            sre.SpeechRecognized += new EventHandler<SpeechRecognizedEventArgs>(sre_SpeechRecognized);
            sre.SpeechHypothesized += new EventHandler<SpeechHypothesizedEventArgs>(sre_SpeechHypothesized);
            sre.SpeechRecognitionRejected += new EventHandler<SpeechRecognitionRejectedEventArgs>(sre_SpeechRecognitionRejected);

            acnxConnection.onConnect += new ActiveCNXConnectionEventHandler(acnxConnection_onConnect);
            acnxConnection.onDisconnect += new ActiveCNXConnectionEventHandler(acnxConnection_onDisconnect);
            acnxConnection.onError += new ActiveCNXErrorEventHandler(acnxConnection_onError);

            this.actionMap = new Dictionary<string,List<Action>>()
            {
                {"play", new List<Action>(){ () => this.acnxConnection.SendDigital(1,0,true), () => this.acnxConnection.SendDigital(0,1,false)}},
                {"stop", new List<Action>(){ () => this.acnxConnection.SendDigital(0,0,true)}},
                {"pause", new List<Action>(){ () => this.acnxConnection.SendDigital(0,3,true), () => this.acnxConnection.SendDigital(0,3,false)}}
            };

        }

        void acnxConnection_onError(object sender, ActiveCNXErrorEventArgs e)
        {
            throw new NotImplementedException(); 
        }

        void acnxConnection_onDisconnect(object sender, ActiveCNXConnectionEventArgs e)
        {
            throw new NotImplementedException();
        }

        void acnxConnection_onConnect(object sender, ActiveCNXConnectionEventArgs e)
        {
            throw new NotImplementedException();
        }

        void sre_SpeechRecognitionRejected(object sender, SpeechRecognitionRejectedEventArgs e)
        {
            this.log.LogEvent("\nSpeech Rejected");
            textBox1.Text = "Speech Rejected \n";
        }

        void sre_SpeechHypothesized(object sender, SpeechHypothesizedEventArgs e)
        {
            this.log.LogEvent(string.Format("\nSpeech Hypothesized: \t{0}", e.Result.Text));
            textBox1.Text = "Speech Hypothesized \n";
        }

        void sre_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            if (actionMap.ContainsKey(e.Result.Text))
            {
                foreach (var del in actionMap[e.Result.Text])
                {
                    del.Invoke();
                }
            }
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            textBox1.Text = "Connect button clicked \n";
        }

        private void btnDisconnect_Click(object sender, EventArgs e)
        {
            textBox1.Text = "Disconnect button clicked \n";
        }

        private void btnSendTest_Click(object sender, EventArgs e)
        {

        }

        private void btnStartVoice_Click(object sender, EventArgs e)
        {
            if (!voiceStarted)
            {
                //check check 123
                //this is a simple test of the gitHub tracking
            }
        }
    }
}
