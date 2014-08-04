using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Forms;

namespace csmathplot
{
    //-----------------------------------------------------------------------------
    // mpWindow
    //-----------------------------------------------------------------------------

    public enum mpID {
        mpID_FIT = 2000,    //!< Fit view to match bounding box of all layers
        mpID_ZOOM_IN,       //!< Zoom into view at clickposition / window center
        mpID_ZOOM_OUT,      //!< Zoom out
        mpID_CENTER,        //!< Center view on click position
        mpID_LOCKASPECT,    //!< Lock x/y scaling aspect
        mpID_GRID,
        mpID_HELP_MOUSE     //!< Shows information about the mouse commands
    };
    public enum mpMouseZoomType {
         mpZOOM_XY          //!< zoom 2 dimensialy
        ,mpZOOM_X
        ,mpZOOM_Y
    };
    public enum wxOrientation
    {
        wxHORIZONTAL = 0x0004,
        wxVERTICAL = 0x0008,
        wxBOTH = wxVERTICAL | wxHORIZONTAL,
        wxORIENTATION_MASK = wxBOTH
    }
    public enum Direction { wxUP, wxLEFT, wxRIGHT, wxDOWN, wxTOP, wxBOTTOM, wxALL }
    public enum wxBitmapType {
      wxBITMAP_TYPE_INVALID, 
      wxBITMAP_TYPE_BMP, 
      wxBITMAP_TYPE_BMP_RESOURCE, 
      wxBITMAP_TYPE_RESOURCE = wxBITMAP_TYPE_BMP_RESOURCE, 
      wxBITMAP_TYPE_ICO, 
      wxBITMAP_TYPE_ICO_RESOURCE, 
      wxBITMAP_TYPE_CUR, 
      wxBITMAP_TYPE_CUR_RESOURCE, 
      wxBITMAP_TYPE_XBM, 
      wxBITMAP_TYPE_XBM_DATA, 
      wxBITMAP_TYPE_XPM, 
      wxBITMAP_TYPE_XPM_DATA, 
      wxBITMAP_TYPE_TIFF, 
      wxBITMAP_TYPE_TIF = wxBITMAP_TYPE_TIFF, 
      wxBITMAP_TYPE_TIFF_RESOURCE, 
      wxBITMAP_TYPE_TIF_RESOURCE = wxBITMAP_TYPE_TIFF_RESOURCE, 
      wxBITMAP_TYPE_GIF, 
      wxBITMAP_TYPE_GIF_RESOURCE, 
      wxBITMAP_TYPE_PNG, 
      wxBITMAP_TYPE_PNG_RESOURCE, 
      wxBITMAP_TYPE_JPEG, 
      wxBITMAP_TYPE_JPEG_RESOURCE, 
      wxBITMAP_TYPE_PNM, 
      wxBITMAP_TYPE_PNM_RESOURCE, 
      wxBITMAP_TYPE_PCX, 
      wxBITMAP_TYPE_PCX_RESOURCE, 
      wxBITMAP_TYPE_PICT, 
      wxBITMAP_TYPE_PICT_RESOURCE, 
      wxBITMAP_TYPE_ICON, 
      wxBITMAP_TYPE_ICON_RESOURCE, 
      wxBITMAP_TYPE_ANI, 
      wxBITMAP_TYPE_IFF, 
      wxBITMAP_TYPE_TGA, 
      wxBITMAP_TYPE_MACCURSOR, 
      wxBITMAP_TYPE_MACCURSOR_RESOURCE, 
      wxBITMAP_TYPE_ANY = 50 
    }
    //-----------------------------------------------------------------------------
    // mpLayer
    //-----------------------------------------------------------------------------
    public enum mpLayerType {
         mpLAYER_UNDEF      //!< Layer type undefined
        ,mpLAYER_AXIS       //!< Axis type layer
        ,mpLAYER_PLOT       //!< Plot type layer
        ,mpLAYER_INFO       //!< Info box type layer
        ,mpLAYER_BITMAP     //!< Bitmap type layer
        ,mpLayer_POINT      //!< Point layer
    } ;

    public enum mpShape
    {
        mpCIRCLE
        ,mpRECT
        ,mpPOINT
        ,mpDIAMOND
        ,mpSQUARE
        ,mpELLIPSE
        ,mpARROW_UP
        ,mpARROW_DOWN
        ,mpARROW_UPDOWN
    };

    public class mpXY
    {
        double x, y;
        mpXY(double _x, double _y){x=_x;y=_y;}
        mpXY(ref mpXY copy){x=copy.x;y=copy.y;}
    };

    /** @name Constants defining mouse modes for mpWindow
    @{*/
    public enum mpMouseMode {
        /** Mouse panning drags the view. Mouse mode for mpWindow. */
        mpMOUSEMODE_DRAG,
        /** Mouse panning creates a zoom box. Mouse mode for mpWindow. */
        mpMOUSEMODE_ZOOMBOX
    }
    /*@}*/

    /** Canvas for plotting mpLayer implementations.

        This class defines a zoomable and moveable 2D plot canvas. Any number
        of mpLayer implementations (scale rulers, function plots, ...) can be
        attached using mpWindow::AddLayer.

        The canvas window provides a context menu with actions for navigating the view.
        The context menu can be retrieved with mpWindow::GetPopupMenu, e.g. for extending it
        externally.

        Since wxMathPlot version 0.03, the mpWindow incorporate the following features:
            - DoubleBuffering (Default=disabled): Can be set with EnableDoubleBuffer
            - Mouse based pan/zoom (Default=enable): Can be set with EnableMousePanZoom.

        The mouse commands can be visualized by the user through the popup menu, and are:
            - Mouse Move+CTRL: Pan (Move)
            - Mouse Wheel: Vertical scroll
            - Mouse Wheel+SHIFT: Horizontal scroll
            - Mouse Wheel UP+CTRL: Zoom in
            - Mouse Wheel DOWN+CTRL: Zoom out

    */
    public partial class mpWindow: Form
    {
        List<mpLayer> m_layers; //!< List of attached plot layers
        Menu m_popmenu;   //!< Canvas' context menu
        bool   m_lockaspect;//!< Scale aspect is locked or not

        bool   m_lockBoundMinY; //!< this lock Y min axis so user cannot move chart over this.
        bool   m_lockBoundMaxY; //!< this lock Y max axis so user cannot move chart over this.
        bool   m_lockBoundMinX; //!< this lock X min axis so user cannot move chart over this.
        bool   m_lockBoundMaxX; //!< this lock X max axis so user cannot move chart over this.


        // bool   m_coordTooltip; //!< Selects whether to show coordinate tooltip
        Color m_bgColour;    //!< Background Colour
        Color m_fgColour;    //!< Foreground Colour
        Color m_axColour;    //!< Axes Colour

        double m_minX;      //!< Global layer bounding box, left border incl.
        double m_maxX;      //!< Global layer bounding box, right border incl.
        double m_minY;      //!< Global layer bounding box, bottom border incl.
        double m_maxY;      //!< Global layer bounding box, top border incl.
        double m_scaleX;    //!< Current view's X scale
        double m_scaleY;    //!< Current view's Y scale

        /*double m_scaleXmin;    //!< X scale minimum value
        double m_scaleXmax;    //!< X scale maximum value
        double m_scaleYmin;    //!< Y scale minimum value
        double m_scaleYmax;    //!< Y scale maximum value
        bool   m_scaleBoundsEnable; //!< enable scale bounds*/

        double m_posX;      //!< Current view's X position
        double m_posY;      //!< Current view's Y position
        int    m_scrX;      //!< Current view's X dimension
        int    m_scrY;      //!< Current view's Y dimension
        int    m_clickedX;  //!< Last mouse click X position, for centering and zooming the view
        int    m_clickedY;  //!< Last mouse click Y position, for centering and zooming the view

        /** These are updated in Fit() only, and may be different from the real borders (layer coordinates) only if lock aspect ratio is true.
          */
        double m_desiredXmin,m_desiredXmax,m_desiredYmin,m_desiredYmax;

        int m_marginTop, m_marginRight, m_marginBottom, m_marginLeft;

        int         m_last_lx,m_last_ly;   //!< For double buffering
        //MemoryDC  m_buff_dc;             //!< For double buffering
        //Bitmap      m_buff_bmp;            //!< For double buffering
        bool        m_enableDoubleBuffer;  //!< For double buffering
        bool        m_enableMouseNavigation;  //!< For pan/zoom with the mouse.
        bool        m_mouseDownHasHappened;     //!< For filtering mouse release event without having had a mouse down event.
        bool        m_enableMousePopup;         //!< For popup menu
        bool        m_mouseMovedAfterRightClick;
        bool        m_mouseMovedAfterMiddleClickWithCtrl;
        int         m_mousePosition_X, m_mousePosition_Y; //!< current mouse position
        int         m_mouseRClick_X, m_mouseRClick_Y; //!< For the right button "drag" feature
        int         m_mouseLClick_X, m_mouseLClick_Y; //!< Starting coords for rectangular zoom selection
        int         m_mouseMClick_X, m_mouseMClick_Y; //!< Starting coords for rectangular zoom selection
        bool        m_enableScrollBars;
        int         m_scrollX, m_scrollY;
        //mpInfoLayer* m_movingInfoLayer;      //!< For moving info layers over the window area
        //mpPointLayer* m_movingPointLayer;    //!< For moving point layers over the graph line

        mpMouseZoomType m_mouseZoomMode;        //!< Default mouse zoom works 2-dimensionsly x+y
        //double      m_MaximumXZoomLevel;         //!< Maximum zoom level.

        bool            m_gradienBackColour;
        Color           m_gradienInitialColour;
        Color           m_gradienDestColour;
        Direction       m_gradienDirect;
        Rectangle       m_zoomingRect;
        bool            m_zoomingHorizontally;

        String    OnMouseHelpString;
        String    OnMouseHelpStringTitle;

        public mpWindow() 
        {
            m_layers = new List<mpLayer>();
            m_popmenu = new MainMenu();
        }
        public mpWindow(ref mpWindow parent, uint id,
                         ref Point pos,
                         ref Size size,
                         long flags = 0, String name = "mathplot")
        {
            m_scaleX = m_scaleY = 1.0;
            m_posX   = m_posY   = 0;
            m_desiredXmin=m_desiredYmin=0;
            m_desiredXmax=m_desiredYmax=1;
            m_scrX   = m_scrY   = 64; // Fixed from m_scrX = m_scrX = 64;
            m_minX   = m_minY   = 0;
            m_maxX   = m_maxY   = 0;
            m_last_lx= m_last_ly= 0;
            //m_buff_bmp = NULL;
            m_enableDoubleBuffer        = false;
            m_enableMouseNavigation     = true;
            m_enableMousePopup          = true;
            m_mouseMovedAfterRightClick = false;
            m_mouseMovedAfterMiddleClickWithCtrl = false;

            //m_movingInfoLayer = NULL;
            //m_movingPointLayer = NULL;
            // Set margins to 0
            m_marginTop = 0; m_marginRight = 0; m_marginBottom = 0; m_marginLeft = 0;
            //SetMaximumXZoomLevel(1);

            m_lockBoundMinY=false;
            m_lockBoundMaxY=false;
            m_lockBoundMinX=false;
            m_lockBoundMaxX=false;

            /*m_scaleXmin = 0.25;
            m_scaleXmax = 2;
            m_scaleYmin = 0.25;
            m_scaleYmax = 2;
            m_scaleBoundsEnable = true;*/

            OnMouseHelpString = "Supported Mouse commands:\n"
                + "- Left button down + Mark area: Rectangular zoom\n"
                + "- Middle button down + Mark are: Horizontal zoom\n"
                + "- Right button down + Move: Pan (Move)\n"
                + "- Wheel: Vertical scroll\n"
                + "- Wheel + SHIFT: Horizontal scroll\n"
                + "- Wheel + CTRL: Zoom in/out";
            OnMouseHelpStringTitle ="Mouse Help";

            m_zoomingRect = new Rectangle();
            m_zoomingHorizontally = true;

            m_popmenu = new MainMenu();


            m_lockaspect = false;

            InitPopupMenu();

            m_layers = new List<mpLayer>();

            m_bgColour = Color.White;
            m_fgColour = Color.Black;


            //defaultly gradien background is disabled but colours defined.
            m_gradienBackColour=true;
            m_gradienInitialColour = Color.FromArgb(150,150,200);
            m_gradienDestColour = Color.FromArgb(255, 255, 255);
            m_gradienDirect = Direction.wxUP;


            m_mouseZoomMode = mpMouseZoomType.mpZOOM_XY;

            m_enableScrollBars = false;
            //SetSizeHints(128, 128);

            // J.L.Blanco: Eliminates the "flick" with the double buffer.
            //SetBackgroundStyle( wxBG_STYLE_CUSTOM );

            UpdateAll();
        }
        ~mpWindow()
        {
            // Free all the layers:
	        DelAllLayers( true, false );
            //wxDELETE(m_buff_bmp);

        }

        /** Init popup menu.
            @todo It might be possible to format the menu outside of class some how...
        */
        void InitPopupMenu()
        {
            //m_popmenu = new Menu();
        }

        /** Get reference to context menu of the plot canvas.
            @return Pointer to menu. The menu can be modified.
        */
        public Menu GetPopupMenu() { return m_popmenu; }

        /** Add a plot layer to the canvas.
            @param layer Pointer to layer. The mpLayer object will get under control of mpWindow,
                         i.e. it will be delete'd on mpWindow destruction
            @param refreshDisplay States whether to refresh the display (UpdateAll) after adding the layer.
            @retval TRUE Success
            @retval FALSE Failure due to out of memory.
        */
        public bool AddLayer(mpLayer layer, bool refreshDisplay = true)
        {
            if (layer != null) {
                m_layers.Add( layer );
                //if( layer.IsPointLayer() ) SetLayerBottom(ref layer);
                if (refreshDisplay) UpdateAll();
                return true;
            };
            return false;

        }

        /** Remove a plot layer from the canvas.
            @param layer Pointer to layer. The mpLayer object will be destructed using delete.
            @param alsoDeleteObject If set to true, the mpLayer object will be also "deleted", not just removed from the internal list.
            @param refreshDisplay States whether to refresh the display (UpdateAll) after removing the layer.
            @return true if layer is deleted correctly

            N.B. Only the layer reference in the mpWindow is deleted, the layer object still exists!
        */
        public bool DelLayer(ref mpLayer layer, bool alsoDeleteObject = false, bool refreshDisplay = true)
        { 
            foreach(mpLayer layIt in m_layers )
            {
    	        if (layIt == layer)
	            {
	                // Also delete the object?
        	        //if (alsoDeleteObject) 
			        //    layIt;
	    	        
                    m_layers.Remove(layIt); // this deleted the reference only
	    	        
                    if (refreshDisplay)
			            UpdateAll();
	    	        return true;
	            }
            }
            return false;
        }

        /** Remove all layers from the plot.
            @param alsoDeleteObject If set to true, the mpLayer objects will be also "deleted", not just removed from the internal list.
            @param refreshDisplay States whether to refresh the display (UpdateAll) after removing the layers.
        */
        public void DelAllLayers(bool alsoDeleteObject, bool refreshDisplay = true)
        {
            while ( m_layers.Count>0 )
            {
		        // Also delete the object?
		        //if (alsoDeleteObject) delete m_layers[0];
		        m_layers.Remove( m_layers.First<mpLayer>() ); // this deleted the reference only
            }
	        if (refreshDisplay)  UpdateAll();
        }


        /*! Get the layer in list position indicated.
            N.B. You <i>must</i> know the index of the layer inside the list!
            @param position position of the layer in the layers list
            @return pointer to mpLayer
        */
        public mpLayer GetLayer(int position)
        {
            if ((position >= (int)m_layers.Count) || position < 0) return null;
            return m_layers[position];
        }

        /*! Get the layer by its name (case sensitive).
            @param name The name of the layer to retrieve
            @return A pointer to the mpLayer object, or NULL if not found.
        */
        public mpLayer GetLayer(ref String name)
        {
            foreach(mpLayer it in m_layers)
                if ( String.Compare(it.GetName(), name ) == 0)
                    return it;
            return null;    // Not found
        }

        /**
        *   CHange drawing order of layer.
        */
        void SetLayerPosition(ref mpLayer layer, int position)
        {
            List<mpLayer> old = m_layers;
            int old_position = 0;
            for (it = old.First(); it != old.Last(); it++, old_position++)
            { if (*it == layer) break; }
            if (it == old.Last()) return; //not found
            if (position == old_position) return; //already in this position
            m_layers.Clear();
            for (int i = 0; i < (int)old.size(); i++)
            {
                if (i == old_position) continue;
                if (i == position) m_layers.Add(layer);
                m_layers.Add(old[i]);
            }
        }
        /*
        void SetLayerBottom(ref mpLayer layer);
        void SetLayerTop(ref mpLayer layer);
        */

        /** Get current view's X scale.
            See @ref mpLayer::Plot "rules for coordinate transformation"
            @return Scale
        */
        double GetXscl() { return m_scaleX; }
        double GetScaleX() { return m_scaleX; } // Schaling's method: maybe another method esists with the same name

        /** Get current view's Y scale.
            See @ref mpLayer::Plot "rules for coordinate transformation"
            @return Scale
        */
        double GetYscl() { return m_scaleY; }
        double GetScaleY()  { return m_scaleY; } // Schaling's method: maybe another method esists with the same name

        /** Get current view's X position.
            See @ref mpLayer::Plot "rules for coordinate transformation"
            @return X Position in layer coordinate system, that corresponds to the center point of the view.
        */
        double GetXpos()  { return m_posX; }
        double GetPosX()  { return m_posX; }

        /** Get current view's Y position.
            See @ref mpLayer::Plot "rules for coordinate transformation"
            @return Y Position in layer coordinate system, that corresponds to the center point of the view.
        */
        double GetYpos()  { return m_posY; }
        double GetPosY()  { return m_posY; }

        /** Get current view's X dimension in device context units.
            Usually this is equal to wxDC::GetSize, but it might differ thus mpLayer
            implementations should rely on the value returned by the function.
            See @ref mpLayer::Plot "rules for coordinate transformation"
            @return X dimension.
        */
        int GetScrX()  { return m_scrX; }
        int GetXScreen()  { return m_scrX; }

        /** Get current view's Y dimension in device context units.
            Usually this is equal to wxDC::GetSize, but it might differ thus mpLayer
            implementations should rely on the value returned by the function.
            See @ref mpLayer::Plot "rules for coordinate transformation"
            @return Y dimension.
        */
        int GetScrY()  { return m_scrY; }
        int GetYScreen()  { return m_scrY; }

        /** Set current view's X scale and refresh display.
            @param scaleX New scale, must not be 0.
        */
        void SetScaleX(double scaleX)
        {
            if (scaleX != 0) m_scaleX = scaleX;
            UpdateAll();
        }

        /** Set current view's Y scale and refresh display.
            @param scaleY New scale, must not be 0.
        */
        void SetScaleY(double scaleY) { if (scaleY!=0) m_scaleY=scaleY; UpdateAll(); }

        /** Set current view's X position and refresh display.
            @param posX New position that corresponds to the center point of the view.
        */
        void SetPosX(double posX) { m_posX=posX; UpdateAll(); }

        /** Set current view's Y position and refresh display.
            @param posY New position that corresponds to the center point of the view.
        */
        void SetPosY(double posY) { m_posY=posY; UpdateAll(); }

        /** Set current view's X and Y position and refresh display.
            @param posX New position that corresponds to the center point of the view.
            @param posY New position that corresponds to the center point of the view.
        */
        void SetPos( double posX, double posY) { m_posX=posX; m_posY=posY; UpdateAll(); }

        /** Set current view's dimensions in device context units.
            Needed by plotting functions. It doesn't refresh display.
            @param scrX New position that corresponds to the center point of the view.
            @param scrY New position that corresponds to the center point of the view.
        */
        void SetScr( int scrX, int scrY) { m_scrX=scrX; m_scrY=scrY; }

        /** Set mouse zoom mode
         */
        void SetMouseZoomMode(mpMouseZoomType t){m_mouseZoomMode = t;}

        /** Get mouse zoom mode
         */
        mpMouseZoomType GetMouseZoomMode(){ return m_mouseZoomMode;}

        /** Converts mpWindow (screen) pixel coordinates into graph (floating point) coordinates, using current mpWindow position and scale.
          * @sa p2y,x2p,y2p */
    //     double p2x(wxCoord pixelCoordX, bool drawOutside = true ); // { return m_posX + pixelCoordX/m_scaleX; }
        double p2x(int pixelCoordX ) { return m_posX + pixelCoordX/m_scaleX; }

        /** Converts mpWindow (screen) pixel coordinates into graph (floating point) coordinates, using current mpWindow position and scale.
          * @sa p2x,x2p,y2p */
    //     double p2y(wxCoord pixelCoordY, bool drawOutside = true ); //{ return m_posY - pixelCoordY/m_scaleY; }
        double p2y(int pixelCoordY ) { return m_posY - pixelCoordY/m_scaleY; }

        /** Converts graph (floating point) coordinates into mpWindow (screen) pixel coordinates, using current mpWindow position and scale.
          * @sa p2x,p2y,y2p */
    //     wxCoord x2p(double x, bool drawOutside = true); // { return (wxCoord) ( (x-m_posX) * m_scaleX); }
        int x2p(double x) { return  (int)( (x-m_posX) * m_scaleX); }

        /** Converts graph (floating point) coordinates into mpWindow (screen) pixel coordinates, using current mpWindow position and scale.
          * @sa p2x,p2y,x2p */
    //     wxCoord y2p(double y, bool drawOutside = true); // { return (wxCoord) ( (m_posY-y) * m_scaleY); }
        int y2p(double y) { return  (int)( (m_posY-y) * m_scaleY); }


        /** Enable/disable the double-buffering of the window, eliminating the flicker (default=disabled).
         */
        void EnableDoubleBuffer( bool enabled ) { m_enableDoubleBuffer = enabled; }

        /** Enable/disable the feature of pan/zoom with the mouse (default=enabled)
         */
        void EnableMousePanZoom( bool enabled ) { m_enableMouseNavigation = enabled; }
        /** Enable/disable the feature of popup with the mouse (default=enabled)
         */
        void EnableMousePopup( bool enabled ) { m_enableMousePopup = enabled; }

        /** Enable or disable X/Y scale aspect locking for the view.
            @note Explicit calls to mpWindow::SetScaleX and mpWindow::SetScaleY will set
                  an unlocked aspect, but any other action changing the view scale will
                  lock the aspect again.
        */
        void LockAspect(bool enable = true)
        {
            m_lockaspect = enable;
            //m_popmenu.Check(mpID_LOCKASPECT, enable);

            // Try to fit again with the new config:
            Fit(m_desiredXmin, m_desiredXmax, m_desiredYmin, m_desiredYmax);
        }

        void SetBoundLockMinY( bool _lock ) { m_lockBoundMinY = _lock; }
        void SetBoundLockMaxY( bool _lock ) { m_lockBoundMaxY = _lock; }
        void SetBoundLockMinX( bool _lock ) { m_lockBoundMinX = _lock; }
        void SetBoundLockMaxX( bool _lock ) { m_lockBoundMaxX = _lock; }

        /** Checks whether the X/Y scale aspect is locked.
            @retval TRUE Locked
            @retval FALSE Unlocked
        */
        public bool IsAspectLocked() { return m_lockaspect; }

        /** Set view to fit global bounding box of all plot layers and refresh display.
            Scale and position will be set to a show all attached mpLayers.
            The X/Y scale aspect lock is taken into account.
        */
        void Fit()
        {
            if (UpdateBBox())
                Fit(m_minX, m_maxX, m_minY, m_maxY);
        }

        /** Set view to fit a given bounding box and refresh display.
            The X/Y scale aspect lock is taken into account.
            If provided, the parameters printSizeX and printSizeY are taken as the DC size, and the
            pixel scales are computed accordingly. Also, in this case the passed borders are not saved 
            as the "desired borders", since this use will be invoked only when printing.
        */
        void Fit(double xMin, double xMax, double yMin, double yMax, int printSizeX = 0, int printSizeY = 0)
        {
            // Save desired borders:
	        m_desiredXmin=xMin; m_desiredXmax=xMax;
	        m_desiredYmin=yMin; m_desiredYmax=yMax;

	        if (printSizeX!=0 && printSizeY!=0)
	        {
		        // Printer:
		        m_scrX = printSizeX;
		        m_scrY = printSizeY;
	        }
	        else
	        {
		        // Normal case (screen):
                //GetClientSize( &m_scrX,&m_scrY);
                m_scrX = this.Width;
                m_scrY = this.Height;
	        }

	        double Ax,Ay;

	        Ax = xMax - xMin;
	        Ay = yMax - yMin;

	        m_scaleX = (Ax!=0) ? (m_scrX - m_marginLeft - m_marginRight)/Ax : 1; //m_scaleX = (Ax!=0) ? m_scrX/Ax : 1;
	        m_scaleY = (Ay!=0) ? (m_scrY - m_marginTop - m_marginBottom)/Ay : 1; //m_scaleY = (Ay!=0) ? m_scrY/Ay : 1;

	        if (m_lockaspect)
	        {
        
	            //wxLogMessage(_("mpWindow::Fit()(lock) m_scaleX=%f,m_scaleY=%f"), m_scaleX,m_scaleY);
        
		        // Keep the lowest "scale" to fit the whole range required by that axis (to actually "fit"!):
		        double s = m_scaleX < m_scaleY ? m_scaleX : m_scaleY;
		        m_scaleX = s;
		        m_scaleY = s;
	        }

	        // Adjusts corner coordinates: This should be simply:
	        //   m_posX = m_minX;
	        //   m_posY = m_maxY;
	        // But account for centering if we have lock aspect:
	        m_posX = (xMin+xMax)/2 - ((m_scrX - m_marginLeft - m_marginRight)/2 + m_marginLeft)/m_scaleX ; // m_posX = (xMin+xMax)/2 - (m_scrX/2)/m_scaleX;
        //	m_posY = (yMin+yMax)/2 + ((m_scrY - m_marginTop - m_marginBottom)/2 - m_marginTop)/m_scaleY;  // m_posY = (yMin+yMax)/2 + (m_scrY/2)/m_scaleY;
	        m_posY = (yMin+yMax)/2 + ((m_scrY - m_marginTop - m_marginBottom)/2 + m_marginTop)/m_scaleY;  // m_posY = (yMin+yMax)/2 + (m_scrY/2)/m_scaleY;

        
	        //wxLogMessage(_("mpWindow::Fit() m_desiredXmin=%f m_desiredXmax=%f  m_desiredYmin=%f m_desiredYmax=%f"), xMin,xMax,yMin,yMax);
	        //wxLogMessage(_("mpWindow::Fit() m_scaleX = %f , m_scrX = %d,m_scrY=%d, Ax=%f, Ay=%f, m_posX=%f, m_posY=%f"), m_scaleX, m_scrX,m_scrY, Ax,Ay,m_posX,m_posY);
        

	        // It is VERY IMPORTANT to DO NOT call Refresh if we are drawing to the printer!!
	        // Otherwise, the DC dimensions will be those of the window instead of the printer device
            if (printSizeX == 0 || printSizeY == 0)
		        UpdateAll();
        }

        /** Zoom into current view and refresh display
          * @param centerPoint The point (pixel coordinates) that will stay in the same position on the screen after the zoom (by default, the center of the mpWindow).
          */
        void ZoomIn(ref Point c)
        {
	        if (c == null)
	        {
		        //GetClientSize(&m_scrX, &m_scrY);
                m_scrX = this.Width;
                m_scrY = this.Height;

		        c.X = (m_scrX - m_marginLeft - m_marginRight)/2 + m_marginLeft; // c.x = m_scrX/2;
		        c.Y = (m_scrY - m_marginTop - m_marginBottom)/2 - m_marginTop; // c.y = m_scrY/2;
	        }

	        // Preserve the position of the clicked point:
	        double prior_layer_x = p2x( c.X );
	        double prior_layer_y = p2y( c.Y );

	        // Zoom in:
	        m_scaleX *= zoomIncrementalFactor;
	        m_scaleY *= (m_mouseZoomMode==mpMouseZoomType.mpZOOM_XY ? zoomIncrementalFactor : 1 );

	        // Adjust the new m_posx/y:
	        m_posX = prior_layer_x - c.X / m_scaleX;
	        m_posY = prior_layer_y + c.Y / m_scaleY;

	        m_desiredXmin = m_posX;
	        m_desiredXmax = m_posX + (m_scrX - m_marginLeft - m_marginRight) / m_scaleX; // m_desiredXmax = m_posX + m_scrX / m_scaleX;
	        m_desiredYmax = m_posY;
	        m_desiredYmin = m_posY - (m_scrY - m_marginTop - m_marginBottom) / m_scaleY; // m_desiredYmin = m_posY - m_scrY / m_scaleY;


	        //wxLogMessage(_("mpWindow::ZoomIn() prior coords: (%f,%f), new coords: (%f,%f) SHOULD BE EQUAL!!"), prior_layer_x,prior_layer_y, p2x(c.x),p2y(c.y));

	        UpdateAll();
        }

        /** Zoom out current view and refresh display
          * @param centerPoint The point (pixel coordinates) that will stay in the same position on the screen after the zoom (by default, the center of the mpWindow).
          */
        void ZoomOut(ref Point c)
        {
	        if (c == null)
	        {
		        //GetClientSize(&m_scrX, &m_scrY);
                m_scrX = this.Width;
                m_scrY = this.Height;
		        c.X = (m_scrX - m_marginLeft - m_marginRight)/2 + m_marginLeft; // c.x = m_scrX/2;
		        c.Y = (m_scrY - m_marginTop - m_marginBottom)/2 - m_marginTop; // c.y = m_scrY/2;
	        }

	        // Preserve the position of the clicked point:
	        double prior_layer_x = p2x( c.X );
	        double prior_layer_y = p2y( c.Y );
	        /*double  tmpPosX = m_posX,
                    tmpPosY = m_posY,
                    tmpDesXmin = m_desiredXmin,
                    tmpDesXmax = m_desiredXmax,
                    tmpDesYmin = m_desiredYmin,
                    tmpDesYmax = m_desiredYmax,
                    tmpScaleX = m_scaleX,
                    tmpScaleY = m_scaleY;*/

	        // Zoom out:

	        /*if(m_scaleBoundsEnable)
	        {
	            if(m_scaleXmax > (m_scaleX / zoomIncrementalFactor) );
                    m_scaleX /= zoomIncrementalFactor;

	            if( m_scaleYmax > ( m_scaleY / (m_mouseZoomMode==mpZOOM_XY ? zoomIncrementalFactor : 1)  ) )
                    m_scaleY /= (m_mouseZoomMode==mpZOOM_XY ? zoomIncrementalFactor : 1);

	        }
	        else*/
	        {
                m_scaleX /= zoomIncrementalFactor;
                m_scaleY /= (m_mouseZoomMode==mpMouseZoomType.mpZOOM_XY ? zoomIncrementalFactor : 1);
            }

	        // Adjust the new m_posx/y:
	        m_posX = prior_layer_x - c.X / m_scaleX;

	        if(m_mouseZoomMode==mpMouseZoomType.mpZOOM_XY)
                m_posY = prior_layer_y + c.Y / m_scaleY;

	        m_desiredXmin = m_posX;
	        m_desiredXmax = m_posX + (m_scrX - m_marginLeft - m_marginRight) / m_scaleX; // m_desiredXmax = m_posX + m_scrX / m_scaleX;
	        m_desiredYmax = m_posY;
	        m_desiredYmin = m_posY - (m_scrY - m_marginTop - m_marginBottom) / m_scaleY; // m_desiredYmin = m_posY - m_scrY / m_scaleY;

	        /*if(m_lockBoundMaxX && m_desiredXmax > m_maxX){
                m_posX = tmpPosX;
                m_scaleX = tmpScaleX;
                m_desiredXmax = tmpDesXmax;
            }
            if(m_lockBoundMinX && m_desiredXmin < m_minX){
                m_posX = tmpPosX;
                m_scaleX = tmpScaleX;
                m_desiredXmin = tmpDesXmin;
            }
            if(m_lockBoundMaxY && m_desiredYmax > m_maxY){
                m_posX = tmpPosY;
                m_scaleY = tmpScaleY;
                m_desiredYmax = tmpDesYmax;
            }
            if(m_lockBoundMinY && m_desiredYmin < m_minY){
                m_posY = tmpPosY;
                m_scaleY = tmpScaleY;
                m_desiredYmin = tmpDesYmin;
            }*/

	        //wxLogMessage(_("mpWindow::ZoomOut() prior coords: (%f,%f), new coords: (%f,%f) SHOULD BE EQUAL!!"), prior_layer_x,prior_layer_y, p2x(c.x),p2y(c.y));
        
	        UpdateAll();
        }

        /** Zoom in current view along X and refresh display */
        void ZoomInX()
        {
            m_scaleX = m_scaleX * zoomIncrementalFactor;
            UpdateAll();
        }
        /** Zoom out current view along X and refresh display */
        void ZoomOutX()
        {
            m_scaleX = m_scaleX / zoomIncrementalFactor;
            UpdateAll();
        }
        /** Zoom in current view along Y and refresh display */
        void ZoomInY()
        {
            m_scaleY = m_scaleY * zoomIncrementalFactor;
            UpdateAll();
        }
        /** Zoom out current view along Y and refresh display */
        void ZoomOutY()
        {
            m_scaleY = m_scaleY / zoomIncrementalFactor;
            UpdateAll();
        }

        /** Zoom view fitting given coordinates to the window (p0 and p1 do not need to be in any specific order) */
        void ZoomRect(Point p0, Point p1)
        {
            // Compute the 2 corners in graph coordinates:
	        double p0x = p2x(p0.X);
	        double p0y = p2y(p0.Y);
	        double p1x = p2x(p1.X);
	        double p1y = p2y(p1.Y);

	        // Order them:
	        double zoom_x_min = p0x<p1x ? p0x:p1x;
	        double zoom_x_max = p0x>p1x ? p0x:p1x;
	        double zoom_y_min = p0y<p1y ? p0y:p1y;
	        double zoom_y_max = p0y>p1y ? p0y:p1y;

        
	        //wxLogMessage(_("Zoom: (%f,%f)-(%f,%f)"),zoom_x_min,zoom_y_min,zoom_x_max,zoom_y_max);
        

	        Fit(zoom_x_min,zoom_x_max,zoom_y_min,zoom_y_max);
        }

        void GetClientSize(out int x, out int y)
        {
            x = this.Width;
            y = this.Height;
        }

        /** Refresh display */
        void UpdateAll()
        {
            if (UpdateBBox())
            {
                if (m_enableScrollBars)
                {
                    int cx, cy;
                    GetClientSize( out cx, out cy);
                    // Do x scroll bar
                    {
                        // Convert margin sizes from pixels to coordinates
                        double leftMargin  = m_marginLeft / m_scaleX;
                        // Calculate the range in coords that we want to scroll over
                        double maxX = (m_desiredXmax > m_maxX) ? m_desiredXmax : m_maxX;
                        double minX = (m_desiredXmin < m_minX) ? m_desiredXmin : m_minX;
                        if ((m_posX + leftMargin) < minX)
                            minX = m_posX + leftMargin;
                        // Calculate scroll bar size and thumb position
                        int sizeX = (int) ((maxX - minX) * m_scaleX);
                        int thumbX = (int)(((m_posX + leftMargin) - minX) * m_scaleX);
                        //SetScrollbar(wxHORIZONTAL, thumbX, cx - (m_marginRight + m_marginLeft), sizeX);
                    }
                    // Do y scroll bar
                    {
                        // Convert margin sizes from pixels to coordinates
                        double topMargin = m_marginTop / m_scaleY;
                        // Calculate the range in coords that we want to scroll over
                        double maxY = (m_desiredYmax > m_maxY) ? m_desiredYmax : m_maxY;
                        if ((m_posY - topMargin) > maxY)
                            maxY = m_posY - topMargin;
                        double minY = (m_desiredYmin < m_minY) ? m_desiredYmin : m_minY;
                        // Calculate scroll bar size and thumb position
                        int sizeY = (int)((maxY - minY) * m_scaleY);
                        int thumbY = (int)((maxY - (m_posY - topMargin)) * m_scaleY);
                        //SetScrollbar(wxVERTICAL, thumbY, cy - (m_marginTop + m_marginBottom), sizeY);
                    }
                }
            }

            //Refresh( false );
        }

        // Added methods by Davide Rondini

        /** Counts the number of plot layers, excluding axes or text: this is to count only the layers which have a bounding box.
            \return The number of profiles plotted.
        */
        uint CountLayers()
        {
            uint layerNo = 0;
            foreach(mpLayer li in m_layers)
    	    {
                if (li.HasBBox()) layerNo++;
    	    };
            return layerNo;
        }

        /** Counts the number of plot layers, whether or not they have a bounding box.
            \return The number of layers in the mpWindow. */
        int CountAllLayers() { return m_layers.Count; }

        /** Draws the mpWindow on a page for printing
            \param print the mpPrintout where to print the graph */
        //void PrintGraph(mpPrintout *print);


        /** Returns the left-border layer coordinate that the user wants the mpWindow to show (it may be not exactly the actual shown coordinate in the case of locked aspect ratio).
            * @sa Fit
            */
        double GetDesiredXmin() {return m_desiredXmin; }

        /** Returns the right-border  layer coordinate that the user wants the mpWindow to show (it may be not exactly the actual shown coordinate in the case of locked aspect ratio).
            * @sa Fit
            */
        double GetDesiredXmax() {return m_desiredXmax; }

        /** Returns the bottom-border  layer coordinate that the user wants the mpWindow to show (it may be not exactly the actual shown coordinate in the case of locked aspect ratio).
            * @sa Fit
            */
        double GetDesiredYmin() {return m_desiredYmin; }

        /** Returns the top layer-border  coordinate that the user wants the mpWindow to show (it may be not exactly the actual shown coordinate in the case of locked aspect ratio).
            * @sa Fit
            */
        double GetDesiredYmax() {return m_desiredYmax; }

        /** Returns the bounding box coordinates
                @param bbox Pointer to a 6-element double array where to store bounding box coordinates. */
        void GetBoundingBox(ref double[] bbox)
        {
            bbox[0] = m_minX;
	        bbox[1] = m_maxX;
	        bbox[2] = m_minY;
	        bbox[3] = m_maxY;
        }

        /** Enable/disable scrollbars
          @param status Set to true to show scrollbars */
        void SetMPScrollbars(bool status)
        {
            // Temporary behaviour: always disable scrollbars
            m_enableScrollBars = status; //false;
            if (status == false)
            {
                //SetScrollbar(wxOrientation.wxHORIZONTAL, 0, 0, 0);
                //SetScrollbar(wxOrientation.wxVERTICAL, 0, 0, 0);
            }
            // else the scroll bars will be updated in UpdateAll();
            UpdateAll();
        }

        /** Get scrollbars status.
          @return true if scrollbars are visible */
        bool GetMPScrollbars() {return m_enableScrollBars; }

        /** Draw the window on a wxBitmap, then save it to a file.
          @param filename File name where to save the screenshot
          @param type image type to be saved: see wxImage output file types for flags
              @param imageSize Set a size for the output image. Default is the same as the screen size
              @param fit Decide whether to fit the plot into the size*/
        //bool SaveScreenshot(ref String filename, int type, Size imageSize, bool fit = false);

        /** This value sets the zoom steps whenever the user clicks "Zoom in/out" or performs zoom with the mouse wheel.
          *  It must be a number above unity. This number is used for zoom in, and its inverse for zoom out. Set to 1.5 by default. */
        static double zoomIncrementalFactor;

        //void SetMaximumXZoomLevel(double level){ m_MaximumXZoomLevel = level; }

        /** Set window margins, creating a blank area where some kind of layers cannot draw. This is useful for example to draw axes outside the area where the plots are drawn.
            @param top Top border
            @param right Right border
            @param bottom Bottom border
            @param left Left border */
        void SetMargins(int top, int right, int bottom, int left)
        {
            m_marginTop = top;
            m_marginRight = right;
            m_marginBottom = bottom;
            m_marginLeft = left;
        }
        /** Set the top margin. @param top Top Margin */
        void SetMarginTop(int top) { m_marginTop = top; }
        /** Set the right margin. @param right Right Margin */
        void SetMarginRight(int right) { m_marginRight = right; }
        /** Set the bottom margin. @param bottom Bottom Margin */
        void SetMarginBottom(int bottom) { m_marginBottom = bottom; }
        /** Set the left margin. @param left Left Margin */
        void SetMarginLeft(int left) { m_marginLeft = left; }

        /** Get the top margin. @param top Top Margin */
        int GetMarginTop() { return m_marginTop; }
        /** Get the right margin. @param right Right Margin */
        int GetMarginRight() { return m_marginRight; }
        /** Get the bottom margin. @param bottom Bottom Margin */
        int GetMarginBottom() { return m_marginBottom; }
        /** Get the left margin. @param left Left Margin */
        int GetMarginLeft() { return m_marginLeft; }

        /** Enable / disable gradien backcolour */
        void SetGradienBackColour(bool b){ m_gradienBackColour=b; }
        /** Set gradien background initial colour */
        void SetGradienInitialColour(Color colour){ m_gradienInitialColour = colour; }
        /** Set gradien background destination colour */
        void SetGradienDestColour(Color colour){ m_gradienDestColour = colour; }
        /**
        *   Set gradien background direction
        * wxUP | wxLEFT | ?wxRIGHT | ?wxUP | ?wxDOWN | ?wxTOP | ?wxBOTTOM | ?wxNORTH | ?wxSOUTH | ?wxWEST | ?wxEAST | ?wxALL
        */
        void SetGradienDirect(Direction direct){ m_gradienDirect = direct; }



        /** Sets whether to show coordinate tooltip when mouse passes over the plot. \param value true for enable, false for disable */
        // void EnableCoordTooltip(bool value = true);
        /** Gets coordinate tooltip status. \return true for enable, false for disable */
        // bool GetCoordTooltip() { return m_coordTooltip; };

        /** Check if a given point is inside the area of a mpInfoLayer and eventually returns its pointer.
            @param point The position to be checked
            @return If an info layer is found, returns its pointer, NULL otherwise */
        //mpInfoLayer* IsInsideInfoLayer(wxPoint& point);

        //mpPointLayer* IsInsidePointLayer(wxPoint& point);

        /** Sets the visibility of a layer by its name.
                @param name The layer name to set visibility
                @param viewable the view status to be set */
        void SetLayerVisible(ref String name, bool viewable)
        {
            mpLayer lx = GetLayer(ref name);
            if (lx != null)
            {
                lx.SetVisible(viewable);
                UpdateAll();
            }
        }

        /** Check whether a layer with given name is visible
                @param name The layer name
                @return layer visibility status */
        bool IsLayerVisible(ref String name)
        {
            mpLayer lx = GetLayer(ref name);
            return (lx!=null) ? lx.IsVisible() : false;
        }

        /** Sets the visibility of a layer by its position in layer list.
                @param position The layer position in layer list
                @param viewable the view status to be set */
        public void SetLayerVisible(int position, bool viewable)
        {
            mpLayer lx = GetLayer(position);
            if (lx != null)
            {
                lx.SetVisible(viewable);
                UpdateAll();
            }
        }

        /** Check whether the layer at given position is visible
                @param position The layer position in layer list
                @return layer visibility status */
        bool IsLayerVisible(int position)
        {
            mpLayer lx = GetLayer(position);
            return (lx!=null) ? lx.IsVisible() : false;
        }
        /** Set Color theme. Provide colours to set a new colour theme.
            @param bgColour Background colour
                @param drawColour The colour used to draw all elements in foreground, axes excluded
                @param axesColour The colour used to draw axes (but not their labels) */
        void SetColourTheme(ref Color bgColour, ref Color drawColour, ref Color axesColour)
        {
            
            this.m_bgColour = bgColour;
            this.m_fgColour = drawColour;
	        
	        m_bgColour = bgColour;
	        m_fgColour = drawColour;
	        m_axColour = axesColour;
	        // cycle between layers to set colours and properties to them
            
            foreach (mpLayer li in m_layers) {
		        if (li.GetLayerType() == mpLayerType.mpLAYER_AXIS) {
			        Pen axisPen = li.GetPen(); // Get the old pen to modify only colour, not style or width
			        axisPen.Color = axesColour;
			        li.SetPen(axisPen);

			        //mpScaleX scale = (mpScaleX)li;
			        //scale->SetTicksColour( axesColour );

		        }
		        else if (li.GetLayerType() == mpLayerType.mpLAYER_INFO) {
			        Pen infoPen = li.GetPen(); // Get the old pen to modify only colour, not style or width
			        infoPen.Color = drawColour;
			        li.SetPen(infoPen);
		        }
		        else if (li.GetLayerType() == mpLayerType.mpLAYER_PLOT) {
                    Pen plotPen = li.GetPen();  //Get the old pen to modify only colour, not style or width
                    plotPen.Color = drawColour;
                    li.SetPen(plotPen);

                    Brush plotBrush = li.GetBrush(); //Get old brush
                    //plotBrush.Color = drawColour;
                    li.SetBrush( plotBrush );
		        }
		        else if (li.GetLayerType() == mpLayerType.mpLayer_POINT) {

		        }
		        else{
		            Brush plotBrush = li.GetBrush(); //Get old brush
                    //plotBrush.Color = drawColour;
                    li.SetBrush( plotBrush );

		            Pen plotPen = li.GetPen();  //Get the old pen to modify only colour, not style or width
                    plotPen.Color =  drawColour;
                    li.SetPen(plotPen);
		        }
	        }
        
        }

        /** Get axes draw colour
                @return reference to axis colour used in theme */
        Color GetAxesColour() { return m_axColour; }


        void SetHelpString(String msg, String title){ OnMouseHelpString=msg; OnMouseHelpStringTitle=title;}

    
        /*
        protected void OnPaint         (wxPaintEvent     &event); //!< Paint handler, will plot all attached layers
        protected void OnSize          (wxSizeEvent      &event); //!< Size handler, will update scroll bar sizes
        // void OnScroll2       (wxScrollWinEvent &event); //!< Scroll handler, will move canvas

        protected void OnCenter        (wxCommandEvent   &event); //!< Context menu handler
        protected void OnFit           (wxCommandEvent   &event); //!< Context menu handler
        protected void OnZoomIn        (wxCommandEvent   &event); //!< Context menu handler
        protected void OnZoomOut       (wxCommandEvent   &event); //!< Context menu handler
        protected void OnLockAspect    (wxCommandEvent   &event); //!< Context menu handler
        protected void OnMouseHelp     (wxCommandEvent   &event); //!< Context menu handler
        protected void OnMouseWheel    (wxMouseEvent     &event); //!< Mouse handler for the wheel
        protected void OnMouseMove     (wxMouseEvent     &event); //!< Mouse handler for mouse motion (for pan)
        protected void OnMouseLeftDown (wxMouseEvent     &event); //!< Mouse left click (for rect zoom)
        protected void OnMouseLeftRelease (wxMouseEvent  &event); //!< Mouse left click (for rect zoom)
        protected void OnMouseRightDown(wxMouseEvent     &event); //!< Mouse handler, for detecting when the user drags with the right button or just "clicks" for the menu
        protected void OnMouseRightRelease(wxMouseEvent  &event); //!< Mouse right up
        protected void OnMouseMiddleDown(wxMouseEvent     &event); //!< Mouse middle down
        protected void OnMouseMiddleRelease(wxMouseEvent  &event); //!< Mouse middle up
        protected void OnShowPopupMenu (wxMouseEvent     &event); //!< Mouse handler, will show context menu
        protected void OnMouseLeaveWindow(wxMouseEvent   &event); //!< Mouse leaves window

        protected void OnScrollThumbTrack (wxScrollWinEvent &event); //!< Scroll thumb on scroll bar moving
        protected void OnScrollPageUp     (wxScrollWinEvent &event); //!< Scroll page up
        protected void OnScrollPageDown   (wxScrollWinEvent &event); //!< Scroll page down
        protected void OnScrollLineUp     (wxScrollWinEvent &event); //!< Scroll line up
        protected void OnScrollLineDown   (wxScrollWinEvent &event); //!< Scroll line down
        protected void OnScrollTop        (wxScrollWinEvent &event); //!< Scroll to top
        protected void OnScrollBottom     (wxScrollWinEvent &event); //!< Scroll to bottom

        protected void DoScrollCalc    (const int position, const int orientation);

        protected void DoZoomInXCalc   (const int         staticXpixel);
        protected void DoZoomInYCalc   (const int         staticYpixel);
        protected void DoZoomOutXCalc  (const int         staticXpixel);
        protected void DoZoomOutYCalc  (const int         staticYpixel);
        */

        /** Recalculate global layer bounding box, and save it in m_minX,...
          * \return true if there is any valid BBox information.
          */
        protected virtual bool UpdateBBox()
        {
            bool first = true;

            foreach(mpLayer f in m_layers )
            {

                if (f.HasBBox() && f.IsVisible()) //updated: If not visible, don't check bounding boxes! 10.11.-09 by Jussi V-A
                {
                    if (first)
                    {
                        first = false;
                        m_minX = f.GetMinX(); m_maxX=f.GetMaxX();
                        m_minY = f.GetMinY(); m_maxY=f.GetMaxY();
                    }
                    else
                    {
                        if (f.GetMinX()<m_minX) m_minX=f.GetMinX(); if (f.GetMaxX()>m_maxX) m_maxX=f.GetMaxX();
                        if (f.GetMinY()<m_minY) m_minY=f.GetMinY(); if (f.GetMaxY()>m_maxY) m_maxY=f.GetMaxY();
                    }
                }
                //node = node->GetNext();
            }

	        //wxLogDebug(wxT("[mpWindow::UpdateBBox] Bounding box: Xmin = %f, Xmax = %f, Ymin = %f, YMax = %f"), m_minX, m_maxX, m_minY, m_maxY);

            return first == false;
        }

    }
}
