using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Collector.Communication.Modbus.RTU;
using System.IO.Ports;
using System.Threading;
using Collector.Communication.Common;

namespace Test
{
    public partial class RTU : Form
    {
        public RTU()
        {
            InitializeComponent();

            _rtu = new ModbusRTU("COM1", 9600,  8, StopBits.One,Parity.None, Application.StartupPath + @"\LogFile", @"\Log.txt");

            _bitOperator = new BitOperator();

            _rtu.Connect();
        }
        private ModbusRTU _rtu ;

        private BitOperator _bitOperator;

        private bool[] boolRes;
        private ushort[] shortRes;
        
        private void Main_Load(object sender, EventArgs e)
        {
            Task.Run(async() =>
            {
                while (true)
                {
                    await Task.Delay(100);

                    //var res = await _rtu.ReadCoils(1, 0, 10);
                    //myLED1.Value = res.Value[0];
                    //myLED2.Value = res.Value[1];
                    //myLED3.Value = res.Value[2];
                    //myLED4.Value = res.Value[3];
                    //myLED5.Value = res.Value[4];
                    //myLED6.Value = res.Value[5];
                    //myLED7.Value = res.Value[6];
                    //myLED8.Value = res.Value[7];
                    //myLED9.Value = res.Value[8];
                    //myLED10.Value = res.Value[9];

                    //var res = await _rtu.ReadHoldingRegisters(1, 0, 6);
                    //if (res != null)
                    //{
                    //    myInstrument1.Value = res.Value[0];
                    //    myInstrument2.Value = res.Value[1];
                    //    myInstrument3.Value = res.Value[2];
                    //    myInstrument4.Value = res.Value[3];
                    //}

                    //bool boolRes = await _rtu.WriteSingleCoil(1,0,true);
                    //bool boolRes = await _rtu.WriteSingleRegister(1, 1, 123);


                    //bool boolRes = await _rtu.WriteMultipleRegisters(1, 0, 5, new ushort[] { 10, 10, 67, 34, 56, 22, 45, 89, 90, 87, 84, 28 });

                    bool boolRes = await _rtu.WriteMultipleCoils(1, 0, 5, new bool[] {true,true,true,false,true,true,false,false,true });

                }
            });
        }

        private void Main_FormClosing(object sender, FormClosingEventArgs e)
        {
            _rtu.DisConnect();
        }
    }
}
