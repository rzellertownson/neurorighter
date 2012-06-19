using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NeuroRighter.Filters
{
    using rawType = System.Double;
    class LocalFit
    {
        
        //user set variables, mentioned in the paper:
        private int N; //segment half-length- referred to as 'tau' in DAW's code.  When approximating the artifact with a polynomial, how many samples out in either direction from the current sample you are approximating do you want to go?
        private int tau;//how Daniel's code uses N

        //the following items are used in Daniel's algorithm, but are accounted for through other terms in this implementation:

        //private int sigma; //estimator width- when you depeg, your fit won't be perfect immmediately.  Deviation from the fit can be measured with an estimator, which uses sigma samples to estimate the deviation.  Default is 1/10th of N.
        //private double beta;//threshold for the estimator- how good does the fit have to be in order to qualify as 'depegged'?  
        //private double threshold; //noise level for each channel

        //user set variables, not mentioned in the paper, but included in the meabench implementation:
        private int t_ahead;//how far ahead should we be looking for pegs (beyond the half width)?
        private int t_blankdepeg;//even after we depeg, SALPA holds the blank down for good measure.  What is the maximum amount of time this should last?
        private int TOOPOORCNT = 10;//how many samples do we have to go through in the too poor state?

        //user set variables introduced in NeuroRighter, because Steve likes knobs and buttons
        private rawType railLow;//what counts for a peg low?
        private rawType railHigh;//what counts for a peg high?
       

        //stuff that the user is not allowed to touch:
        private state elecState;
        private rawType[] dest;
        private rawType[] source;
        private int t0;//when does the peg occur?
        private int t_stream;//what sample are we currently on?
        private int t_limit;//how far are we allowed to go?
        private int toopoorcnt;//how many samples do we have left to go in the too poor state?
        //stuff from NR implementation:
        private rawType[] previousData;//data from previous bufferload
        
        private int PRE;//number of samples we need before we can start processing
        private int POST;//number of samples we need after we finish processing
        private int bufferlength;

        //curve variables
        private double alpha0;
        private double alpha1;
        private double alpha2;
        private double alpha3;
        private rawType my_thresh;
        private rawType y_threshold;
        private int t_chi2;
        private int forcepegsamples;

        //if you come up with a nice explanation for these, please put it here:
        private double X0;
        private double X1;
        private double X2;
        private double X3;
        private bool negv;

        //often used terms
        private int tau_plus_1;
        private int tau_plus_1_squared;
        private int tau_plus_1_cubed;
        private int minus_tau;
        private int minus_tau_squared;
        private int minus_tau_cubed;
        private double T0,T2,T4,T6;//these can get huge.
        private bool toPeg;//riley added
        private int delaycnt;
  

        public LocalFit(rawType y_threshold, int N, int t_blankdepeg, int t_ahead, int t_chi2, rawType railHigh,rawType railLow,int bufferlength, int forcepegsamples)
        {
            elecState = state.PEGGED;
            this.N = N;
            this.tau = N;
            this.y_threshold = y_threshold;
            this.t_blankdepeg = t_blankdepeg;
            this.t_ahead = t_ahead;
            this.t_chi2 = t_chi2;
            this.railHigh = railHigh;
            this.railLow = railLow;
            this.forcepegsamples = forcepegsamples;
            //this code in the init_t section of DAW's MB
            my_thresh = 3.92 * t_chi2 * y_threshold;

            //a bunch of terms that get used repeatedly:
            tau_plus_1 = tau + 1;
            tau_plus_1_squared = tau_plus_1 * tau_plus_1;
            tau_plus_1_cubed = tau_plus_1_squared * tau_plus_1;
            minus_tau = -tau;
            minus_tau_squared = minus_tau * minus_tau;
            minus_tau_cubed = minus_tau_squared * minus_tau;

            T0 = T2 = T4 = T6 = 0;
            for (int t = -tau; t <= tau; t++)
            {
                int t2 = t * t;
                int t4 = t2 * t2;
                int t6 = t4 * t2;
                T0 += 1;
                T2 += t2;
                T4 += t4;
                T6 += t6;
            }
            this.bufferlength = bufferlength;
            PRE = 2 * N;
            POST = 2 * N + 1+t_ahead;
            source = new rawType[PRE + POST + bufferlength];
            previousData = new rawType[PRE + POST];

        }

        public rawType[] filter(ref List<int> stimIndices, ref rawType[][] filtData, int channel)
        {
            //grab old data from previous buffer load
            for (int i = 0; i < PRE + POST; i++)
            {
                source[i] = previousData[i];
            }

            //grab new data from this buffer load
            for (int i = 0; i < bufferlength; i++)
            {
                source[i + PRE + POST] = filtData[channel][i];
            }

            //store unused buffer data for next time
            for (int i = 0; i < PRE + POST; i++)
            {
                //if we don't have enough samples in the filtData buffer to fill up previousData, use data already in previousData
                if (i < (PRE + POST) - bufferlength)
                    previousData[i] = previousData[i+bufferlength];
                else
                    previousData[i] = filtData[channel][i + bufferlength - (PRE + POST)];
            }
            
            t_stream = PRE;//what sample are we currently on?
            t_limit = bufferlength+PRE;
            dest = new rawType[bufferlength];
          
            int t_nextPeg;
            int t_processTo;
            toPeg = false;
            
            while (t_stream < t_limit)
            {
                
                //look for pegs between PRE+t_ahead and PRE+POST+bufferlength in the source buffer
                //- this corresponds to -POST+t_ahead through bufferlength in the stimindices/filtdata
                t_nextPeg = t_limit;//if we don't have an upcoming peg
                while (stimIndices.Count > 0)//look for a peg
                {
                    if ((stimIndices[0] +PRE+POST)< (t_ahead +t_stream))
                        stimIndices.RemoveAt(0); //pop off
                    else
                    {
                        toPeg = false;
                        if (stimIndices[0] <= bufferlength + tau + t_ahead)// peg is somewhere we care about
                        {
                            t_nextPeg = stimIndices[0]+PRE+POST;//convert from real samples to buffered samples
                            t_processTo = t_nextPeg;
                            stimIndices.RemoveAt(0);
                            source[t_nextPeg] = railHigh+0.01;//in order to force a peg, I just set the damn trace to railHigh where it should peg.
                            toPeg = true;
                        }
                        break;

                    }
                }
                

                //set limit
                
                t_processTo = Math.Min(t_nextPeg, t_limit);


                while (t_stream < t_processTo)//process up to the next peg
                {
                #region statemachine
                    switch (elecState)
                    {
                        case state.PEGGED://you should only be in this state if you are pegged and passing a zero
                            #region PEGGED
                            //if we actually are pegged right now, keep trucking
                            if (ispegged(source[t_stream]))
                            {
                                dest[t_stream-PRE] = 0;
                                t_stream++;
                                break;
                            }
                            //look ahead 2*N samples for another peg.  If you find one, force a peg
                            for (int dt = 1; dt <= 2 * tau; dt++)
                                if (ispegged(source[t_stream + dt]))
                                {
                                    t0 = t_stream + dt;//the location of the peg
                                    elecState = state.FORCEPEG;
                                    break;
                                }
                            if (elecState == state.FORCEPEG)
                                break;
                            else
                            {
                                t0 = t_stream + tau+1;//location of when we are clear//HACK
                                calc_X012(); calc_X3();
                                calc_alpha0123();
                                toopoorcnt = TOOPOORCNT;
                                delaycnt = 0;
                                elecState = state.TOOPOOR;
                            }
                            #endregion
                            break;
                        case state.FORCEPEG://you should only be in this state is you aren't pegged right now but you will be so soon that it isn't worth trying to fit anything.
                            #region FORCEPEG
                            if (t_stream < t0)
                            {
                                dest[t_stream-PRE] = 0;
                                t_stream++;
                            }
                            else
                            {
                                elecState = state.PEGGED;
                            }

                            #endregion
                            break;
                        case state.TOOPOOR://you should only be in this state if you haven't been able to get an asymmetric fit after a peg
                            #region TOOPOOR
                            //delaycnt = 0;
                            double asym = 0;
                            double sig = 0;

                            //calculate asymmetry
                            for (int i = 0; i < t_chi2; i++)
                            {
                                int t_i = t_stream + i;
                                double dt = t_i - t0;
                                double dt2 = dt * dt;
                                double dt3 = dt * dt2;
                                double dy = alpha0 + alpha1 * dt + alpha2 * dt2 + alpha3 * dt3 - source[t_i];
                                asym += dy;
                                sig += dy * dy;
                            }
                            asym *= asym;
                            if (asym < my_thresh)
                                toopoorcnt--;
                            else
                                toopoorcnt = TOOPOORCNT;

                            //have we met the criteria for a good fit?
                            if ((toopoorcnt <= 0) && (asym < my_thresh / 3.92))
                            {
                                double dt = t_stream - t0;
                                double dt2 = dt * dt;
                                double dt3 = dt * dt2;
                                negv = source[t_stream] < (alpha0 + alpha1 * dt + alpha2 * dt2 + alpha3 * dt3);
                                //int_t x0=X0,x1=X1,x2=X2,x3=X3;//I can't find where these are used.
                                calc_X012(); calc_X3(); // for numerical stability problem!
                                elecState = state.BLANKDEPEG;
                                //System.Console.Write(channel.ToString() + " delay " + (delaycnt).ToString() + ":");
                                break;
                            }

                            //fit isn't good enough yet- blank and go on to next time step
                            dest[t_stream - PRE] = 0;// 1 + asym - my_thresh;
                            t_stream++; t0++; delaycnt++;
                            if (t_stream == t_processTo)
                                break;//
                            //do we need to force a peg?
                            
                            if (ispegged(source[t0 + tau+t_ahead]))
                            {
                                t0 = t0 + tau;
                                elecState = state.FORCEPEG;
                                break;
                            }
                            
                            //if not, recalculate the curve
                            update_X0123();
                            //int_t x0=X0,x1=X1,x2=X2,x3=X3;//I can't find where these lower case values are used
                            calc_X012(); calc_X3(); // for numerical stability problem!
                            calc_alpha0123();
                            #endregion
                            break;
                        case state.BLANKDEPEG://you should only be in this state if you have met the asymmetry requirement of the TOOPOOR state but you still have misgivings about curve fitting.
                            #region BLANKDEPEG
                            {
                                if (t_stream >= t0 - tau + t_blankdepeg)
                                {
                                    elecState = state.DEPEGGING;
                                    break;
                                }


                                //double dt = t_stream - t0;
                                //double dt2 = dt * dt;
                                //double dt3 = dt * dt2;
                                //rawType y = source[t_stream] - (rawType)(alpha0 + alpha1 * dt + alpha2 * dt2 + alpha3 * dt3);
                                //if ((y < 0) != negv)
                                //{
                                //    dest[t_stream-PRE] = y;
                                //    t_stream++;
                                //    elecState = state.DEPEGGING;
                                //    break;
                                //}

                                dest[t_stream-PRE] = 0;
                                t_stream++;
                            }
                            #endregion
                            break;
                        case state.DEPEGGING://you should only be in this state if you are ready to curve fit, but the peg occured as few as N (tau) samples ago
                            #region DEPEGGING
                            {
                                if (t_stream == t0)
                                {
                                    elecState = state.OK;
                                    break;
                                }
                                double dt = t_stream - t0;
                                double dt2 = dt * dt;
                                double dt3 = dt * dt2;
                                dest[t_stream-PRE] = source[t_stream]
                                  - (rawType)(alpha0 + alpha1 * dt + alpha2 * dt2 + alpha3 * dt3);
                                t_stream++;
                            }
                            #endregion
                            break;
                        case state.OK://you should only be in this state if you are ready to curve fit, and you have N (tau) samples in either direction to curve fit with
                            #region OK
                            calc_alpha0();
                            dest[t_stream - PRE] = source[t_stream] - (rawType)(alpha0);//source[t_stream] - X0;//
                            t_stream++;
                            //we are in the okay state- so check to see if going to peg or trigger a peg soon.
                            if (ispegged(source[t_stream + tau + t_ahead]))
                            {
                                t0 = t_stream - 1;
                                calc_X3();
                                calc_alpha0123();
                                elecState = state.PEGGING;
                                break;

                            }
                            update_X012();

                            #endregion
                            break;
                        case state.PEGGING:
                            #region PEGGING
                            {
                                if (t_stream >= t0 + tau)
                                {
                                    //t_peg = t_stream;//used to report a peg
                                    elecState = state.PEGGED;
                                    break;
                                }
                                double dt = t_stream - t0;
                                double dt2 = dt * dt;
                                double dt3 = dt * dt2;
                                dest[t_stream-PRE] = source[t_stream]
                                  - (rawType)(alpha0 + alpha1 * dt + alpha2 * dt2 + alpha3 * dt3);
                                t_stream++;
                            }
                            #endregion
                            break;
                        default:
                            throw new Exception("SALPA error:  sample " + t_stream.ToString() + " with unknown state " + elecState.ToString());
                    }
#endregion
                }

                //we just processed through t_processto
                if (toPeg&(t_processTo==t_nextPeg))
                {
                    t0 = t_stream + forcepegsamples;
                    elecState = state.FORCEPEG;
                    toPeg = false;
                }
                
            }
           
            t0 = t0 - t_stream + PRE;
            
            return dest;
        }

        //functions to actually calculate the curve
        #region X0-3 functions
        private void calc_X012()
         {
             X0 = X1 = X2 = 0;
             for (int t = -tau; t <= tau; t++)
             {
                 int t2 = t * t;
                 rawType y = source[t0 + t];
                 X0 += y;
                 X1 += ((double)t * y);
                 X2 += ((double)t2 * y);
             }
         }
         private void calc_X3()
         {
             X3 = 0;
             for (int t = -tau; t <= tau; t++)
             {
                 int t3 = t * t * t;
                 rawType y = source[t0 + t];
                 X3 += ((double)t3 * y);
             }
         }

         private void update_X0123()
         {
             double y_new = source[t0 + tau];
             double y_old = source[t0 - tau - 1];
             X0 += y_new - y_old;
             X1 += (double)tau_plus_1 * y_new - (double)minus_tau * y_old - X0;
             X2 += (double)tau_plus_1_squared * y_new - (double)minus_tau_squared * y_old - X0 - 2 * X1;
             X3 += (double)tau_plus_1_cubed * y_new - (double)minus_tau_cubed * y_old - X0 - 3 * X1 - 3 * X2;
         }

         private void update_X012()
         {
             double y_new = source[t_stream + tau];
             double y_old = source[t_stream - tau - 1];
             X0 += (y_new - y_old);
             X1 += ((double)tau_plus_1 * y_new - (double)minus_tau * y_old) - X0;
             X2 += ((double)tau_plus_1_squared * y_new - (double)minus_tau_squared * y_old) - X0 - 2 * X1;
         }
        #endregion

         #region alpha0-3 functions
         private void calc_alpha0()
         {
             alpha0 = (double)(T4 * X0 - T2 * X2) / (double)(T0 * T4 - T2 * T2);
         }
         private void calc_alpha0123()
         {
             double fact02 = (T0*T4-T2*T2);
             alpha0 = (T4*X0 - T2*X2)/fact02;
             alpha2 = (double)(T0 * X2 - T2 * X0)/fact02;
             double fact13 =  (T2 * T6 - T4 * T4);
             alpha1 = (double)(T6 * X1 - T4 * X3) / fact13;
             alpha3 = (double)(T2 * X3 - T4 * X1) / fact13;
             
         }
          #endregion


         private bool ispegged(rawType r)
         {
             bool tmp = (r < railLow) || (r > railHigh);
             if (tmp)
             {
                 tmp = true;
             }
             return tmp;
         }

            private enum state
        {
           PEGGED,
           FORCEPEG,
           TOOPOOR,
           BLANKDEPEG,
           DEPEGGING,
           OK,
           PEGGING

        }
    }
}
