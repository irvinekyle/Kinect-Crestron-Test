using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace KinectTest
{
    public partial class Log : Form
    {
        public Log()
        {
            InitializeComponent();
        }

        public void LogEvent(string logText)
        {
            this.txtBoxLog.Text = logText;
        }
    }
}
