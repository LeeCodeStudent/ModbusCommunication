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

            _rtu = new ModbusRTU();

            _bitOperator = new BitOperator();

            _rtu.Connect("COM1", 9600, Parity.None, 8, StopBits.One);
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

                    //boolRes = await _rtu.ReadCoils(1, 0, 10);
                    //myLED1.Value = boolRes[0];
                    //myLED2.Value = boolRes[1];
                    //myLED3.Value = boolRes[2];
                    //myLED4.Value = boolRes[3];
                    //myLED5.Value = boolRes[4];
                    //myLED6.Value = boolRes[5];
                    //myLED7.Value = boolRes[6];
                    //myLED8.Value = boolRes[7];
                    //myLED9.Value = boolRes[8];
                    //myLED10.Value = boolRes[9];

                    //shortRes = await _rtu.ReadHoldingRegisters(1, 0, 6);
                    //if (shortRes != null)
                    //{
                    //    myInstrument1.Value = shortRes[0];
                    //    myInstrument2.Value = shortRes[1];
                    //    myInstrument3.Value = shortRes[2];
                    //    myInstrument4.Value = shortRes[3];
                    //    boolRes = _bitOperator.GetBitArrayFromByteArray(new byte[] { (byte)shortRes[4], (byte)shortRes[5] }, true);
                    //    myLED1.Value = boolRes[0];
                    //    myLED2.Value = boolRes[1];
                    //    myLED3.Value = boolRes[2];
                    //    myLED4.Value = boolRes[3];
                    //    myLED5.Value = boolRes[4];
                    //    myLED6.Value = boolRes[5];
                    //    myLED7.Value = boolRes[6];
                    //    myLED8.Value = boolRes[7];
                    //    myLED9.Value = boolRes[8];
                    //    myLED10.Value = boolRes[9];
                    //}

                    //bool boolRes = await _rtu.WriteSingleCoil(1,1,true);
                    //bool boolRes = await _rtu.WriteSingleRegister(1, 1, 123);


                    //bool boolRes1 = await _rtu.WriteMultipleRegisters(1, 0, 5, new ushort[] { 10, 10, 67, 34, 56, 22, 45, 89, 90, 87, 84, 28 });

                    //short[] xx = _bitOperator.GetShortArrayFromByteArray(new byte[] {0x00, 0x45, 0x00, 0x06, 0x00, 0x34 });
                    //for (int i = 0; i < xx.Length; i++)
                    //{
                    //    Console.WriteLine(xx[i]);
                    //}


                }
            });
            //byte res = _bitOperator.GetByteFrom8Bits(new bool[] {true,true,true,true,false,false,false,false  });
            ////for (int i = 0; i < res.Length; i++)
            ////{
            ////    Console.Write(Convert.ToInt16(res[i]));
            ////}
            //Console.Write(res);

        }

        private void Main_FormClosing(object sender, FormClosingEventArgs e)
        {
            _rtu.DisConnect();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Task.Run(async () =>
            {
                bool boolRes1 = await _rtu.WriteMultipleCoils(1, 0, 4, new bool[] { false, true, true, true, true, true, false, false, true, true, true, false });
            });
        }
    }
}
