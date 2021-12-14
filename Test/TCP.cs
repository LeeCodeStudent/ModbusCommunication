using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Collector.Communication.Modbus;
using Collector.Communication.Modbus.TCP;
using System.Threading;
using Collector.Common.File;

namespace Test
{
    public partial class TCP : Form
    {
        public TCP()
        {
            InitializeComponent();

        }
        private ModbusTCP _tcp;

        private XMLOperater _xml;


        private void TCP_Load(object sender, EventArgs e)
        {
            _xml = new XMLOperater(Application.StartupPath + @"\Config\Config.xml", "root");
            _xml.LoadXml();
            List<Config> tcpList = _xml.GetChildNodeInner<Config>("TCPConfig");
            _tcp = new ModbusTCP(tcpList[0].IP, tcpList[1].Port, Application.StartupPath + @"\LogFile", @"\Log.txt");
            var result = _tcp.Connect();
            Task.Run(async() =>
            {
                while (true)
                {
                    await Task.Delay(100);
                    var res = await _tcp.ReadHoldingRegisters(1, 0, 6);
                    myInstrument1.Value = res.Value[0];
                    myInstrument2.Value = res.Value[1];
                    myInstrument3.Value = res.Value[2];
                    myInstrument4.Value = res.Value[3];
                    myInstrument5.Value = res.Value[4];
                    myInstrument6.Value = res.Value[5];

                    //bool[] resbool = await _tcp.ReadDiscreteInputs(1,0,6);
                    //myLED1.Value = resbool[0];
                    //myLED2.Value = resbool[1];
                    //myLED3.Value = resbool[2];
                    //myLED4.Value = resbool[3];
                    //myLED5.Value = resbool[4];
                    //myLED6.Value = resbool[5];


                    //bool resbool = await _tcp.WriteSingleCoil(1,0,false);
                    //bool resbool = await _tcp.WriteSingleRegister(1, 0, 678);

                    //bool resbool = await _tcp.WriteMultipleCoils(1,0,4,new bool[] { true,true,false,true});
                    //bool resbool = await _tcp.WriteMultipleRegisters(1, 0, 4, new ushort[] { 12, 23, 45, 34, 78, 67, 90, 76 });
                }
            });
        }

        private void TCP_FormClosing(object sender, FormClosingEventArgs e)
        {
            _tcp.DisConnect();
        }
        public class Config
        {
            public string IP;
            public int Port;
        }
    }
}
