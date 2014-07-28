using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace csmathplot
{
    public class mpText : mpLayer
    {

        int m_offsetx; //!< Holds offset for X in percentage
        int m_offsety; //!< Holds offset for Y in percentage

    
        /** @param name text to be drawn in the plot
            @param offsetx holds offset for the X location in percentage (0-100)
            @param offsety holds offset for the Y location in percentage (0-100) */
        public mpText(String name = "Title", int offsetx = 5, int offsety = 50)
        {
            SetName(name);

            if (offsetx >= 0 && offsetx <= 100)
                m_offsetx = offsetx;
            else
                m_offsetx = 5;

            if (offsety >= 0 && offsety <= 100)
                m_offsety = offsety;
            else
                m_offsetx = 50;
            m_type = mpLayerType.mpLAYER_INFO;
        }

        /** Text Layer plot handler.
            This implementation will plot text adjusted to the visible area. */
        public override void Plot(ref Graphics dc, ref mpWindow w)
        {
            if (m_visible)
            {
                /*dc.Pen(m_pen);
                dc.SetFont(m_font);

                Point tw = 0, th = 0;
                dc.GetTextExtent(GetName(), &tw, &th);*/

                //     int left = -dc.LogicalToDeviceX(0);
                //     int width = dc.LogicalToDeviceX(0) - left;
                //     int bottom = dc.LogicalToDeviceY(0);
                //     int height = bottom - -dc.LogicalToDeviceY(0);

                /*    dc.DrawText( GetName(),
                    (int)((((float)width/100.0) * m_offsety) + left - (tw/2)),
                    (int)((((float)height/100.0) * m_offsetx) - bottom) );*/
                int px = m_offsetx * (w.Width - w.Left - w.Right) / 100;
                int py = m_offsety * (w.Height - w.Top - w.Bottom) / 100;

                dc.DrawString(GetName(), m_font, m_brush, px, py);
                
            }
        }

        /** mpText should not be used for scaling decisions. */
        public virtual bool HasBBox() { return false; }

    
        
    };
}
