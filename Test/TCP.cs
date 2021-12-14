using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Collector.Communication.Modbus.TCP;
using System.Threading;

namespace Test
{
    public partial class TCP : Form
    {
        public TCP()
        {
            InitializeComponent();

            _tcp = new ModbusTCP();
        }
        private ModbusTCP _tcp;
        private void TCP_Load(object sender, EventArgs e)
        {
            if (_tcp.Connect("127.0.0.1", 502))
            {
                Console.WriteLine("连接成功");
            }
            Task.Run(async() =>
            {
                while (true)
                {
                    await Task.Delay(100);
                    //short[] res = await _tcp.ReadInputRegisters(1, 0, 6);
                    //myInstrument1.Value = res[0];
                    //myInstrument2.Value = res[1];
                    //myInstrument3.Value = res[2];
                    //myInstrument4.Value = res[3];
                    //myInstrument5.Value = res[4];
                    //myInstrument6.Value = res[5];

                    //bool[] resbool = await _tcp.ReadDiscreteInputs(1,0,6);
                    //myLED1.Value = resbool[0];
                    //myLED2.Value = resbool[1];
                    //myLED3.Value = resbool[2];
                    //myLED4.Value = resbool[3];
                    //myLED5.Value = resbool[4];
                    //myLED6.Value = resbool[5];

                    //bool resbool = await _tcp.WriteMultipleCoils(1,0,4,new bool[] { true,true,false,true});
                    bool resbool = await _tcp.WriteMultipleRegisters(1, 0, 4, new ushort[] { 12, 23, 45, 34, 78, 67, 90, 76 });

                }
            });
        }
    }
}
