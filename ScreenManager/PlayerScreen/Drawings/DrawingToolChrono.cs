/*
Copyright � Joan Charmant 2008.
joan.charmant@gmail.com 
 
This file is part of Kinovea.

Kinovea is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License version 2 
as published by the Free Software Foundation.

Kinovea is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with Kinovea. If not, see http://www.gnu.org/licenses/.
*/

using System;
using System.Drawing;
using System.Windows.Forms;

namespace Kinovea.ScreenManager
{
    public class DrawingToolChrono : AbstractDrawingTool
    {
		public override DrawingType DrawingType
        {
        	get { return DrawingType.Chrono; }
        }
		public override bool Attached
        {
        	get { return false; }
        }
		
        public DrawingToolChrono()
        {
        }
        public override AbstractDrawing GetNewDrawing(Point _Origin, long _iTimestamp, long _AverageTimeStampsPerFrame)
        {
            return new DrawingChrono(_Origin.X, _Origin.Y, _iTimestamp, _AverageTimeStampsPerFrame);
        }
        public override DrawingToolType OnMouseUp()
        {
            // Fall back to Pointer tool.
            return DrawingToolType.Pointer;
        }
        public override Cursor GetCursor(Color _color, int _iSize)
        {
            return Cursors.Cross;
        }
    }
}
