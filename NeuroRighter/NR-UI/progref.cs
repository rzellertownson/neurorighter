// PROGREF.CS
// Copyright (c) 2008-2011 John Rolston
//
// This file is part of NeuroRighter.
//
// NeuroRighter is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// NeuroRighter is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with NeuroRighter.  If not, see <http://www.gnu.org/licenses/>.

//#define USE_LOG_FILE
//#define DEBUG

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.IO;
using System.IO.Ports;
using System.Runtime.InteropServices;
using NationalInstruments;
using NationalInstruments.DAQmx;
using NationalInstruments.UI;
using NationalInstruments.UI.WindowsForms;
using NationalInstruments.Analysis;
using NationalInstruments.Analysis.Dsp;
using NationalInstruments.Analysis.Dsp.Filters;
using NationalInstruments.Analysis.Math;
using NationalInstruments.Analysis.SignalGeneration;
using csmatio.types;
using csmatio.io;
using rawType = System.Double;


namespace NeuroRighter
{

    ///<summary>Methods for using programmable referencing.</summary>
    ///<author>John Rolston</author>
    sealed internal partial class NeuroRighter : Form
    {
        private void changeReference(int type)
        {
            //'type' is 0 for spikes, 1 for LFPs
            int ch = 0;
            if (type == 0)
                ch = Convert.ToInt32(numericUpDown_analogRefSpikes.Value);
            else if (type == 1)
                ch = Convert.ToInt32(numericUpDown_analogRefLFPs.Value) + 32; //32 comes from the 32 channel preamp
            else if (type == 2)   //Reset spike refs
            {
                serialOut.Write("#0140/3," + currentRef[0].ToString() + "\r");
                for (int i = 1; i <= 32; ++i)
                {
                    serialOut.Write("#0140/5," + i.ToString() + "\r");
                    serialOut.Write("#0140/4," + i.ToString() + ",8\r");
                }
                return;
            }
            else if (type == 3)  //Reset LFP refs
            {
                serialOut.Write("#0140/3," + currentRef[1].ToString() + "\r");
                for (int i = 33; i <= 64; ++i)
                {
                    serialOut.Write("#0140/5," + i.ToString() + "\r");
                    serialOut.Write("#0140/4," + i.ToString() + ",8\r");
                }
                return;
            }

            if (currentRef[type] > 0)
            {
                serialOut.Write("#0140/3," + currentRef[type].ToString() + "\r"); //Disconnect old ch from ref1
                //serialOut.Write("#0140/3," + (currentRef+32).ToString() + "\r"); //Disconnect old ch from ref1
            }
            currentRef[type] = ch;
            serialOut.Write("#0140/3," + currentRef[type].ToString() + "\r"); //Disconnect new ch from any ref

            if (type == 0)
                serialOut.Write("#0140/2," + currentRef[type].ToString() + ",1\r"); //Set 'ch' to ref1
            else if (type == 1)
                serialOut.Write("#0140/2," + currentRef[type].ToString() + ",2\r"); //Set 'ch' to ref1

            //Set ref channel's ref to default
            serialOut.Write("#0140/5," + currentRef[type].ToString() + "\r");
            serialOut.Write("#0140/4," + currentRef[type].ToString() + ",8\r");
            //Likewise for LFPs
            //serialOut.Write("#0140/5," + (currentRef + 32).ToString() + "\r");
            //serialOut.Write("#0140/4," + (currentRef + 32).ToString() + ",8\r");


            //Now, set all other channel's reference's to ref1
            if (type == 0)
            {
                for (int i = 1; i < currentRef[type]; ++i)
                {
                    serialOut.Write("#0140/5," + i.ToString() + "\r");
                    serialOut.Write("#0140/4," + i.ToString() + ",1\r");
                }
                for (int i = currentRef[type] + 1; i <= 32; ++i)
                {
                    serialOut.Write("#0140/5," + i.ToString() + "\r");
                    serialOut.Write("#0140/4," + i.ToString() + ",1\r");
                }
            }
            else if (type == 1)
            {
                //Now, do the same things for LFP channels
                for (int i = 33; i < currentRef[type]; ++i)
                {
                    serialOut.Write("#0140/5," + i.ToString() + "\r");
                    serialOut.Write("#0140/4," + i.ToString() + ",2\r");
                }
                for (int i = currentRef[type] + 1; i <= 64; ++i)
                {
                    serialOut.Write("#0140/5," + i.ToString() + "\r");
                    serialOut.Write("#0140/4," + i.ToString() + ",2\r");
                }
            }

        }

        private void numericUpDown_analogRefSpikes_ValueChanged(object sender, EventArgs e)
        {
            if (checkBox_analogRefSpikes.Checked)
                changeReference(0); //Send 0 for spikes, 1 for LFPs
        }

        private void numericUpDown_analogRefLFPs_ValueChanged(object sender, EventArgs e)
        {
            if (checkBox_analogRefSpikes.Checked)
                changeReference(1); //Send 0 for spikes, 1 for LFPs
        }

        private void checkBox_analogRefSpikes_CheckedChanged(object sender, EventArgs e)
        {
            if (!checkBox_analogRefSpikes.Checked)
                changeReference(0);
            else
            {
                //serialOut.Write("#0140/0\r"); //Reset everything to power-up state
                changeReference(2);
                currentRef[0] = 0;
            }
        }

        private void checkBox_analogRefLFPs_CheckedChanged(object sender, EventArgs e)
        {
            if (!checkBox_analogRefSpikes.Checked)
                changeReference(1);
            else
            {
                //serialOut.Write("#0140/0\r"); //Reset everything to power-up state
                changeReference(3);
                currentRef[1] = 0;
            }
        }

        private void button_analogResetRefs_Click(object sender, EventArgs e)
        {
            serialOut.Write("#0140/0\r"); //Reset everything to power-up state
        }

        private void radioButton_spikesReferencingCommonAverage_CheckedChanged(object sender, EventArgs e)
        {
            lock (this)
            {
                if (radioButton_spikesReferencingCommonAverage.Checked)
                    referncer = new Filters.CommonAverageReferencer(spikeBufferLength);
            }
        }

        private void radioButton_spikesReferencingCommonMedian_CheckedChanged(object sender, EventArgs e)
        {
            lock (this)
            {
                if (radioButton_spikesReferencingCommonMedian.Checked)
                    referncer = new Filters.CommonMedianReferencer(spikeBufferLength, numChannels);
            }
        }

        private void radioButton_spikesReferencingCommonMedianLocal_CheckedChanged(object sender, EventArgs e)
        {
            int channelsPerGroup = Convert.ToInt32(numericUpDown_CommonMedianLocalReferencingChannelsPerGroup.Value);
            lock (this)
            {
                if (radioButton_spikesReferencingCommonMedianLocal.Checked)
                    referncer = new Filters.CommonMedianLocalReferencer(spikeBufferLength, channelsPerGroup, numChannels / channelsPerGroup);
            }
        }

        private void radioButton_spikeReferencingNone_CheckedChanged(object sender, EventArgs e)
        {
            lock (this)
            {
                if (radioButton_spikeReferencingNone.Checked)
                    referncer = null;
            }
        }

        private void numericUpDown_CommonMedianLocalReferencingChannelsPerGroup_ValueChanged(object sender, EventArgs e)
        {
            int channelsPerGroup = Convert.ToInt32(numericUpDown_CommonMedianLocalReferencingChannelsPerGroup.Value);
            if (numChannels % channelsPerGroup != 0)
            {
                channelsPerGroup = 8;
                numericUpDown_CommonMedianLocalReferencingChannelsPerGroup.Value = 8;
                MessageBox.Show("Value must evenly divide total number of channels.");
            }
            if (radioButton_spikesReferencingCommonMedianLocal.Checked)
                referncer = new Filters.CommonMedianLocalReferencer(spikeBufferLength, channelsPerGroup, numChannels / channelsPerGroup);


        }
    }
}
