using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace mat_290_framework
{
    public partial class MAT290 : Form
    {
        public MAT290()
        {
            InitializeComponent();

            pointCalculations = new List<List<Point2D>>();
            chooseTable = new long[40,40];
            deCastlejauTableP1 = new Point2D[40,40];
            Array.Clear(chooseTable, 0, chooseTable.Length);
            // Array.Clear(deCastlejauTableP1, -1, deCastlejauTableP1.Length);
        
            for(int i=0;i<40;i++)
            {
                for(int j=0;j<40;j++)
                {
                    deCastlejauTableP1[i, j] = new Point2D(-1, -1);
                }
            }


            pts_ = new List<Point2D>();   
            tVal_ = 0.5F;
            degree_ = 0;
            knot_ = new List<float>();
            EdPtCont_ = true;
            rnd_ = new Random();

            //Project 1
            myCoefPoints = new List<Point2D>();
            //proj1Degree = 1;
        }

        // Point class for general math use
        protected class Point2D : System.Object
        {
            public float x;
            public float y;

            public Point2D(float _x, float _y)
            {
                x = _x;
                y = _y;
            }

            public Point2D(Point2D rhs)
            {
                x = rhs.x;
                y = rhs.y;
            }

            // adds two points together; used for barycentric combos
            public static Point2D operator +(Point2D lhs, Point2D rhs)
            {
                return new Point2D(lhs.x + rhs.x, lhs.y + rhs.y);
            }

            // gets a distance between two points. not actual distance; used for picking
            public static float operator %(Point2D lhs, Point2D rhs)
            {
                float dx = (lhs.x - rhs.x);
                float dy = (lhs.y - rhs.y);

                return (dx * dx + dy * dy);
            }

            // scalar multiplication of points; for barycentric combos
            public static Point2D operator *(float t, Point2D rhs)
            {
                return new Point2D(rhs.x * t, rhs.y * t);
            }

            // scalar multiplication of points; for barycentric combos
            public static Point2D operator *(Point2D rhs, float t)
            {
                return new Point2D(rhs.x * t, rhs.y * t);
            }

            // returns the drawing subsytems' version of a point for drawing.
            public System.Drawing.Point P()
            {
                return new System.Drawing.Point((int)x, (int)y);
            }
        };

        List<Point2D> pts_; // the list of points used in internal algthms
        float tVal_; // t-value used for shell drawing
        int degree_; // degree of deboor subsplines
        List<float> knot_; // knot sequence for deboor
        bool EdPtCont_; // end point continuity flag for std knot seq contruction
        Random rnd_; // random number generator
        List<List<Point2D>> pointCalculations;
        long[,] chooseTable;
        Point2D[,] deCastlejauTableP1;
        List<Point2D> myCoefPoints;
      //  int proj1Degree;


        // pickpt returns an index of the closest point to the passed in point
        //  -- usually a mouse position
        private int PickPt(Point2D m)
        {
            float closest = m % pts_[0];
            int closestIndex = 0;

            for (int i = 1; i < pts_.Count; ++i)
            {
                float dist = m % pts_[i];
                if (dist < closest)
                {
                    closest = dist;
                    closestIndex = i;
                }
            }

            return closestIndex;
        }

        private void Menu_Clear_Click(object sender, EventArgs e)
        {
            pts_.Clear();

            for (int i = 0; i < 40; i++)
            {
                for (int j = 0; j < 40; j++)
                {
                    deCastlejauTableP1[i, j] = new Point2D(-1, -1);
                }
            }


            Refresh();
        }

        private void Menu_Exit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void MAT290_MouseMove(object sender, MouseEventArgs e)
        {
            // if the right mouse button is being pressed
            if (pts_.Count != 0 && e.Button == MouseButtons.Right)
            {
                if (Project1.Checked)
                {
                    int index = PickPt(new Point2D(e.X, e.Y));
                    float temp = e.Y;
                    
                    if(temp <50)
                    {
                        temp = 50;
                    }
                    if(temp>500)
                    {
                        temp = 500;
                    }

                    pts_[index].y = temp;
                }
                else
              
                {
                    // grab the closest point and snap it to the mouse
                    int index = PickPt(new Point2D(e.X, e.Y));

                    pts_[index].x = e.X;
                    pts_[index].y = e.Y;

                
                }
                Refresh();
            }
        }

        private void MAT290_MouseDown(object sender, MouseEventArgs e)
        {
            // if the left mouse button was clicked
            if (e.Button == MouseButtons.Left)
            {
                 /*
                if (Project1.Checked || Project5.Checked)
                { Refresh(); }
               
                else if (Project5.Checked)
                {

                }
                */
                // add a new point to the controlPoints
              //  else
                {
                    if (!Project1.Checked && !Project5.Checked)
                    { pts_.Add(new Point2D(e.X, e.Y)); }

                    for (int i = 0; i < 40; i++)
                    {
                        for (int j = 0; j < 40; j++)
                        {
                            if (j < pts_.Count)
                            { deCastlejauTableP1[0, j] = pts_[j]; }
                            else
                            {
                                deCastlejauTableP1[i, j] = new Point2D(-1, -1);
                            }
                        }
                    }

                    if (Menu_DeBoor.Checked)
                    {
                        ResetKnotSeq();
                        UpdateKnotSeq();
                    }
                }
                Refresh();
            }

            // if there are points and the middle mouse button was pressed
            if (pts_.Count != 0 && e.Button == MouseButtons.Middle)
            {
                // then delete the closest point
                int index = PickPt(new Point2D(e.X, e.Y));

                pts_.RemoveAt(index);

                if (Menu_DeBoor.Checked)
                {
                    ResetKnotSeq();
                    UpdateKnotSeq();
                }

                Refresh();
            }
        }

        private void MAT290_MouseWheel(object sender, MouseEventArgs e)
        {
            // if the mouse wheel has moved
            if (e.Delta != 0)
            {
                // change the t-value for shell
                tVal_ += e.Delta / 120 * .02f;

                // handle edge cases
                tVal_ = (tVal_ < 0) ? 0 : tVal_;
                tVal_ = (tVal_ > 1) ? 1 : tVal_;

                Refresh();
            }
        }

        private void NUD_degree_ValueChanged(object sender, EventArgs e)
        {
            if (pts_.Count == 0)
                return;

            degree_ = (int)NUD_degree.Value;

            ResetKnotSeq();
            UpdateKnotSeq();

            NUD_degree.Value = degree_;

            Refresh();
        }

        private void CB_cont_CheckedChanged(object sender, EventArgs e)
        {
            EdPtCont_ = CB_cont.Checked;

            ResetKnotSeq();
            UpdateKnotSeq();

            Refresh();
        }

        private void Txt_knot_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\r' || e.KeyChar == '\n')
            {
                // update knot seq
                string[] splits = Txt_knot.Text.ToString().Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

                if (splits.Length > pts_.Count + degree_ + 1)
                    return;

                knot_.Clear();
                foreach (string split in splits)
                {
                    knot_.Add(Convert.ToSingle(split));
                }

                for (int i = knot_.Count; i < (pts_.Count + degree_ + 1); ++i)
                    knot_.Add((float)(i - degree_));

                UpdateKnotSeq();
            }

            Refresh();
        }

        private void Menu_Polyline_Click(object sender, EventArgs e)
        {
            Refresh();
        }

        private void Menu_Points_Click(object sender, EventArgs e)
        {
            Refresh();
        }

        private void Menu_Shell_Click(object sender, EventArgs e)
        {
            Refresh();
        }

        private void Menu_DeCast_Click(object sender, EventArgs e)
        {
            Menu_DeCast.Checked = !Menu_DeCast.Checked;
            Menu_Bern.Checked = false;
            Menu_Midpoint.Checked = false;

            Menu_Inter_Poly.Checked = false;
            Menu_Inter_Splines.Checked = false;

            Menu_DeBoor.Checked = false;

            Menu_Polyline.Enabled = true;
            Menu_Points.Enabled = true;
            Menu_Shell.Enabled = true;

            ToggleDeBoorHUD(false);

            Refresh();
        }
        
        private void Menu_Bern_Click(object sender, EventArgs e)
        {
            Menu_DeCast.Checked = false;
            Menu_Bern.Checked = !Menu_Bern.Checked;
            Menu_Midpoint.Checked = false;

            Menu_Inter_Poly.Checked = false;
            Menu_Inter_Splines.Checked = false;

            Menu_DeBoor.Checked = false;

            Menu_Polyline.Enabled = true;
            Menu_Points.Enabled = true;
            Menu_Shell.Enabled = true;

            ToggleDeBoorHUD(false);

            Refresh();
        }

        private void Menu_Midpoint_Click(object sender, EventArgs e)
        {
            Menu_DeCast.Checked = false;
            Menu_Bern.Checked = false;
            Menu_Midpoint.Checked = !Menu_Midpoint.Checked;

            Menu_Inter_Poly.Checked = false;
            Menu_Inter_Splines.Checked = false;

            Menu_DeBoor.Checked = false;

            Menu_Polyline.Enabled = true;
            Menu_Points.Enabled = true;
            Menu_Shell.Enabled = true;

            ToggleDeBoorHUD(false);

            Refresh();
        }

        private void Menu_Inter_Poly_Click(object sender, EventArgs e)
        {
            Menu_DeCast.Checked = false;
            Menu_Bern.Checked = false;
            Menu_Midpoint.Checked = false;

            Menu_Inter_Poly.Checked = !Menu_Inter_Poly.Checked;
            Menu_Inter_Splines.Checked = false;

            Menu_DeBoor.Checked = false;

            Menu_Polyline.Enabled = false;
            Menu_Polyline.Checked = false;
            Menu_Points.Enabled = true;
            Menu_Shell.Enabled = false;
            Menu_Shell.Checked = false;

            ToggleDeBoorHUD(false);

            Refresh();
        }

        private void Menu_Inter_Splines_Click(object sender, EventArgs e)
        {
            Menu_DeCast.Checked = false;
            Menu_Bern.Checked = false;
            Menu_Midpoint.Checked = false;

            Menu_Inter_Poly.Checked = false;
            Menu_Inter_Splines.Checked = !Menu_Inter_Splines.Checked;

            Menu_DeBoor.Checked = false;

            Menu_Polyline.Enabled = false;
            Menu_Polyline.Checked = false;
            Menu_Points.Enabled = true;
            Menu_Shell.Enabled = false;
            Menu_Shell.Checked = false;

            ToggleDeBoorHUD(false);

            Refresh();
        }

        private void Menu_DeBoor_Click(object sender, EventArgs e)
        {
            Menu_DeCast.Checked = false;
            Menu_Bern.Checked = false;
            Menu_Midpoint.Checked = false;

            Menu_Inter_Poly.Checked = false;
            Menu_Inter_Splines.Checked = false;

            Menu_DeBoor.Checked = !Menu_DeBoor.Checked;

            Menu_Polyline.Enabled = true;
            Menu_Points.Enabled = true;
            Menu_Shell.Enabled = true;

            ToggleDeBoorHUD(true);

            Refresh();
        }

        private void DegreeClamp()
        {
            // handle edge cases
            degree_ = (degree_ > pts_.Count - 1) ? pts_.Count - 1 : degree_;
            degree_ = (degree_ < 1) ? 1 : degree_;
        }

        private void ResetKnotSeq( )
        {
            DegreeClamp();
            knot_.Clear();

            if (EdPtCont_)
            {
                for (int i = 0; i < degree_; ++i)
                    knot_.Add(0.0f);
                for (int i = 0; i <= (pts_.Count - degree_); ++i)
                    knot_.Add((float)i);
                for (int i = 0; i < degree_; ++i)
                    knot_.Add((float)(pts_.Count - degree_));
            }
            else
            {
                for (int i = -degree_; i <= (pts_.Count); ++i)
                    knot_.Add((float)i);
            }
        }

        private void UpdateKnotSeq()
        {
            Txt_knot.Clear();
            foreach (float knot in knot_)
            {
                Txt_knot.Text += knot.ToString() + " ";
            }
        }

        private void ToggleDeBoorHUD( bool on )
        {
            // set up basic knot sequence
            if( on )
            {
                ResetKnotSeq();
                UpdateKnotSeq();
            }

            CB_cont.Visible = on;

            Lbl_knot.Visible = on;
            Txt_knot.Visible = on;

            Lbl_degree.Visible = on;
            NUD_degree.Visible = on;
        }

        private void MAT290_Paint(object sender, PaintEventArgs e)
        {
            // pass the graphics object to the DrawScreen subroutine for processing
            DrawScreen(e.Graphics);
        }

        private void DrawScreen(System.Drawing.Graphics gfx)
        {

            P1DegreeBox.Visible = Project1.Checked;
            P1DegreeLabel.Visible = Project1.Checked; 



            if (Project1.Checked)
            {
                BackgroundImage = (mat_290_framework.Properties.Resources.Project1GraphBackground);
                //Starting point is ~(0,195)
                //Constrain from ~50 - 495 y, 
                //720 px wide

                float f = (float)P1DegreeBox.Value;

                if (pts_.Count != f+1)
                {
                    pts_.Clear();


                    for (int i = 0; i <= f; i++)
                    {

                        pts_.Add(new Point2D((i / f) * 720, 195));
                    }
                }
            }

            else if (Project5.Checked)
            {

            }

            else { BackgroundImage = null; }
            // to prevent unecessary drawing
            if (pts_.Count == 0)// && !Project1.Checked && !Project5.Checked)
            {
               // pts_.Clear();
               // P1DegreeBox.Visible = false;
                //P1DegreeLabel.Visible = false;
                return;
            }
            // pens used for drawing elements of the display
            System.Drawing.Pen polyPen = new Pen(Color.Gray, 1.0f);
            System.Drawing.Pen shellPen = new Pen(Color.LightGray, 0.5f);
            System.Drawing.Pen splinePen = new Pen(Color.Red, 1.5f);

            if (Menu_Shell.Checked)
            {
                // draw the shell
                DrawShell(gfx, shellPen, pts_, tVal_);
            }

            if (Menu_Polyline.Checked)
            {
                // draw the control poly
                for (int i = 1; i < pts_.Count; ++i)
                {
                    gfx.DrawLine(polyPen, pts_[i - 1].P(), pts_[i].P());
                }
            }

            if (Menu_Points.Checked)
            {
                // draw the control points
                foreach (Point2D pt in pts_)
                {
                    gfx.DrawEllipse(polyPen, pt.x - 2.0F, pt.y - 2.0F, 4.0F, 4.0F);
                }
            }

            // you can change these variables at will; i have just chosen there
            //  to be six sample points for every point placed on the screen
            float steps = pts_.Count * 5;
            float alpha = 1 / steps;
            /*
                        List<Point2D> ptsCopy = new List<Point2D>();
                        for(int i=0;i<pts_.Count;i++)
                        {
                            ptsCopy.Add(pts_[i]);
                        }

            */
            ///////////////////////////////////////////////////////////////////////////////
            // Drawing code for algorithms goes in here                                  //
            ///////////////////////////////////////////////////////////////////////////////
            /*
            if (Project1.Checked)
            {
                P1DegreeBox.Visible = true;
                P1DegreeLabel.Visible = true;

                pts_.Clear();
                float f = (float)P1DegreeBox.Value;

                for(int i=0;i<f;i++)
                {
                  
                    pts_.Add(new Point2D( (i / f), 1));
                }


                
                List<Point2D> p1Points = new List<Point2D>();

                for (int i = 0; i < pts_.Count; i++)
                {
                    p1Points.Add(new Point2D(rangeTransform(0, ClientSize.Width, 0, 1, pts_[i].x), rangeTransform(0, ClientSize.Height, -3, 3, pts_[i].y)));

                }

                pts_ = p1Points;
                
            }
            
            else
            
            {
                P1DegreeBox.Visible = false;
                P1DegreeLabel.Visible = false;

            }
            */

            // DeCastlejau algorithm
            if (Menu_DeCast.Checked)
            {
               

                Point2D current_left;
                Point2D current_right = new Point2D(DeCastlejau(0));

                if (Project1.Checked)
                {
                  //  current_right.x = rangeTransform(0, 1, 0, ClientSize.Width, current_right.x);
                  //  current_right.y = rangeTransform(-3, 3, 0, ClientSize.Height, current_right.y);
                }

                for (float t = alpha; t < 1; t += alpha)
                {
                    current_left = current_right;

                    current_right = DeCastlejau(t);
                    if (Project1.Checked)
                    {
                     //   current_right.x = rangeTransform(0, 1, 0, ClientSize.Width, current_right.x);
                     //   current_right.y = rangeTransform(-3, 3, 0, ClientSize.Height, current_right.y);
                    }
                    
                    gfx.DrawLine(splinePen, current_left.P(), current_right.P());
                }

                gfx.DrawLine(splinePen, current_right.P(), DeCastlejau(1).P());
            }

            // Bernstein polynomial
            if (Menu_Bern.Checked)
            {
                Point2D current_left;
                Point2D current_right = new Point2D(Bernstein(0));

                if (Project1.Checked)
                {
                     current_right.x = rangeTransform(0, ClientSize.Width, 0, 1, current_right.x);
                       current_right.y = rangeTransform( 0, ClientSize.Height,-3, 3, current_right.y);
                }

                for (float t = alpha; t < 1; t += alpha)
                {
                    current_left = current_right;
                    current_right = Bernstein(t);


                    if (Project1.Checked)
                    {
                        current_right.x = rangeTransform(0, 1, 10, ClientSize.Width, current_right.x);
                        current_right.y = rangeTransform(-3, 3, 10, ClientSize.Height, current_right.y);
                    }


                    gfx.DrawLine(splinePen, current_left.P(), current_right.P());
                }

                gfx.DrawLine(splinePen, current_right.P(), Bernstein(1).P());
            }

            // Midpoint algorithm
            if (Menu_Midpoint.Checked)
            {
                DrawMidpoint(gfx, splinePen, pts_);
            }

            // polygon interpolation
            if (Menu_Inter_Poly.Checked)
            {
                Point2D current_left;
                Point2D current_right = new Point2D(PolyInterpolate(0));

                for (float t = alpha; t < 1; t += alpha)
                {
                    current_left = current_right;
                    current_right = PolyInterpolate(t);
                    gfx.DrawLine(splinePen, current_left.P(), current_right.P());
                }

                gfx.DrawLine(splinePen, current_right.P(), PolyInterpolate(1).P());
            }

            // spline interpolation
            if (Menu_Inter_Splines.Checked)
            {
                Point2D current_left;
                Point2D current_right = new Point2D(SplineInterpolate(0));

                for (float t = alpha; t < 1; t += alpha)
                {
                    current_left = current_right;
                    current_right = SplineInterpolate(t);
                    gfx.DrawLine(splinePen, current_left.P(), current_right.P());
                }

                gfx.DrawLine(splinePen, current_right.P(), SplineInterpolate(1).P());
            }

            // deboor
            if (Menu_DeBoor.Checked && pts_.Count >= 2)
            {
                Point2D current_left;
                Point2D current_right = new Point2D(DeBoorAlgthm(knot_[degree_]));

                float lastT = knot_[knot_.Count - degree_ - 1] - alpha;
                for (float t = alpha; t < lastT; t += alpha)
                {
                    current_left = current_right;
                    current_right = DeBoorAlgthm(t);
                    gfx.DrawLine(splinePen, current_left.P(), current_right.P());
                }

                gfx.DrawLine(splinePen, current_right.P(), DeBoorAlgthm(lastT).P());
            }
            
            ///////////////////////////////////////////////////////////////////////////////
            // Drawing code end                                                          //
            ///////////////////////////////////////////////////////////////////////////////


            // Heads up Display drawing code

            Font arial = new Font("Arial", 12);

            if (Menu_DeCast.Checked)
            {
                gfx.DrawString("DeCasteljau", arial, Brushes.Black, 0, 30);
            }
            else if (Menu_Midpoint.Checked)
            {
                gfx.DrawString("Midpoint", arial, Brushes.Black, 0, 30);
            }
            else if (Menu_Bern.Checked)
            {
                gfx.DrawString("Bernstein", arial, Brushes.Black, 0, 30);
            }
            else if (Menu_DeBoor.Checked)
            {
                gfx.DrawString("DeBoor", arial, Brushes.Black, 0, 30);
            }

            gfx.DrawString("t-value: " + tVal_.ToString("F"), arial, Brushes.Black, 500, 30);

            gfx.DrawString("t-step: " + alpha.ToString("F6"), arial, Brushes.Black, 600, 30);

            gfx.DrawString(pts_.Count.ToString(), arial, Brushes.Black, 750, 30);
        }

        private void DrawShell(System.Drawing.Graphics gfx, System.Drawing.Pen pen, List<Point2D> pts, float t)
        {

        }

        private Point2D Gamma(int start, int end, float t)
        {
            return new Point2D(0, 0);
        }

        private Point2D DeCastlejau(float t)
        {
            //return new Point2D(t,NLIMethod())
            //return new Point2D(0, 0);

            /*


        public double NLIMethod(List<float> coef, float tVal, int upper, int lower)
        {
            if (upper == 0)
            {
                return coef[lower];
            }
            else
            {
                return ((1 - tVal) * NLIMethod(coef, tVal, upper - 1, lower) + tVal * NLIMethod(coef, tVal, upper - 1, Math.Min(coef.Count, lower + 1)));
            }
        }
        */

            return  NLIMethod(pts_, t, pts_.Count-1, 0);

    }
        
    private Point2D Bernstein(float t,List<Point2D> p)
        {
            return BBform(p, t, p.Count - 1, 0);
        }

        private Point2D DeCastlejau(float t, List<Point2D>p)
        {
            return NLIMethod(p, t, p.Count - 1, 0);
        }

    private Point2D Bernstein(float t)
        {
            // return new Point2D(t, BBform(pts_,t,degree_,0));
          return  BBform(pts_, t, pts_.Count-1, 0);
        }

        private const float MAX_DIST = 6.0F;

        private void DrawMidpoint(System.Drawing.Graphics gfx, System.Drawing.Pen pen, List<Point2D> cPs)
        {

        }

        private Point2D PolyInterpolate(float t)
        {
            return new Point2D(0, 0);
        }

        private Point2D SplineInterpolate(float t)
        {
            return new Point2D(0, 0);
        }

        private Point2D DeBoorAlgthm(float t)
        {
            return new Point2D(0, 0);
        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {

        }

        private void project2ToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void project7ToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }




        private void Project1_Click(object sender, EventArgs e)
        {
            Project1.Checked = true;
            Project2.Checked = false;
            Project3.Checked = false;
            Project4.Checked = false;
            Project5.Checked = false;
            Project6.Checked = false;
            Project7.Checked = false;
            Project8.Checked = false;

            //800 high, 500 long
            //Top, left, right, bot:  50, 100,1100,850


        }

        private void Project2_Click(object sender, EventArgs e)
        {
            Project1.Checked = false;
            Project2.Checked = true;
            Project3.Checked = false;
            Project4.Checked = false;
            Project5.Checked = false;
            Project6.Checked = false;
            Project7.Checked = false;
            Project8.Checked = false;
        }

        private void Project3_Click(object sender, EventArgs e)
        {
            Project1.Checked = false;
            Project2.Checked = false;
            Project3.Checked = true;
            Project4.Checked = false;
            Project5.Checked = false;
            Project6.Checked = false;
            Project7.Checked = false;
            Project8.Checked = false;
        }

        private void Project4_Click(object sender, EventArgs e)
        {
            Project1.Checked = false;
            Project2.Checked = false;
            Project3.Checked = false;
            Project4.Checked = true;
            Project5.Checked = false;
            Project6.Checked = false;
            Project7.Checked = false;
            Project8.Checked = false;
        }

        private void Project5_Click(object sender, EventArgs e)
        {
            Project1.Checked = false;
            Project2.Checked = false;
            Project3.Checked = false;
            Project4.Checked = false;
            Project5.Checked = true;
            Project6.Checked = false;
            Project7.Checked = false;
            Project8.Checked = false;
        }

        private void Project6_Click(object sender, EventArgs e)
        {
            Project1.Checked = false;
            Project2.Checked = false;
            Project3.Checked = false;
            Project4.Checked = false;
            Project5.Checked = false;
            Project6.Checked = true;
            Project7.Checked = false;
            Project8.Checked = false;
        }

        private void Project7_Click(object sender, EventArgs e)
        {
            Project1.Checked = false;
            Project2.Checked = false;
            Project3.Checked = false;
            Project4.Checked = false;
            Project5.Checked = false;
            Project6.Checked = false;
            Project7.Checked = true;
            Project8.Checked = false;
        }

        private void Project8_Click(object sender, EventArgs e)
        {
            Project1.Checked = false;
            Project2.Checked = false;
            Project3.Checked = false;
            Project4.Checked = false;
            Project5.Checked = false;
            Project6.Checked = false;
            Project7.Checked = false;
            Project8.Checked = true;
        }

        private Point2D NLIMethod(List<Point2D> coef, float tValue, int upper, int lower)
        {

     

        //    if (deCastlejauTableP1[upper, lower].x == deCastlejauTableP1[upper, lower].y && deCastlejauTableP1[upper, lower].x == -1)
            {

                if (upper == 0)
                {
                    if (coef.Count > 1)
                    {

                       // deCastlejauTableP1[upper, lower] = coef[lower];
                        return coef[lower];
                    }
                    else
                    {
                      //  deCastlejauTableP1[upper, lower] = coef[0];
                        return coef[0];
                    }
                }
                else
                {
                   // deCastlejauTableP1[upper, lower] = (1 - tValue) * NLIMethod(coef, tValue, upper - 1, lower) + tValue * NLIMethod(coef, tValue, upper - 1, lower + 1);//Math.Min(coef.Count, lower + 1));
                   // return deCastlejauTableP1[upper, lower];

                return (1 - tValue) * NLIMethod(coef, tValue, upper - 1, lower) + tValue * NLIMethod(coef, tValue, upper - 1, lower + 1);
                }
            }

          //  else { return deCastlejauTableP1[upper, lower]; }
        }
        public long nCr(int upper, int lower)
        {
            if (chooseTable[upper, lower] != 0)
            { return chooseTable[upper, lower]; }

            else
            {
                if (lower == 0 || upper == lower)
                {
                    chooseTable[upper, lower] = 1;
                    return 1;
                }
                else if (upper - 1 == lower || lower == 1)
                {
                    chooseTable[upper, upper - 1] = upper;
                    chooseTable[upper, 1] = upper;
                    return upper;
                }

                else
                {
                    chooseTable[upper, lower] = nCr(upper - 1, lower - 1) + nCr(upper - 1, lower);
                    return chooseTable[upper, lower];
                    //return nCr(upper - 1, lower - 1) + nCr(upper - 1, lower);
                }
            }
        }
        private Point2D BBform(List<Point2D> coef, float tValue, int upper, int lower)
        {Point2D o = new Point2D(0,0);
            for(int i=0;i<coef.Count;i++)
            {
                o += (coef[i] * nCr(upper, i) * (float)((Math.Pow((1 - tValue), upper - i) * Math.Pow(tValue, i))));
               // o.y += (coef[i].y * nCr(upper, lower) * (float)(Math.Pow((1 - tValue), upper - lower) * Math.Pow(tValue, lower)));

            }
            if(coef.Count==1)
            { return coef[0]; }
            return o;
        }


        private float rangeTransform(float inputRangeMin, float inputRangeMax, float outputRangeMin, float outputRangeMax, float inputX) 
        {
            return  (((outputRangeMax - outputRangeMin) * (inputX - inputRangeMin)) / (inputRangeMax - inputRangeMin)) + outputRangeMin;
        }

        private void chart1_Click(object sender, EventArgs e)
        {

        }

        private void chart1_Click_1(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void NUD_degree_Click(object sender, EventArgs e)
        {
        //    MessageBox.Show("Hello world.");
        }

        private void MAT290_Load(object sender, EventArgs e)
        {

        }

 

        private void P1DegreeBox_ValueChanged(object sender, EventArgs e)
        {
            // P1DegreeBox.Update();

            //P1DegreeBox.ResetText();
            P1DegreeBox.Refresh();
            Refresh();
        }



        private void button1_Click_1(object sender, EventArgs e)
        {
            Refresh();
        }
    }
}