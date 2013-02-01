﻿#region License
/*
Copyright © Joan Charmant 2013.
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
#endregion
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;

using Kinovea.Camera;
using Kinovea.Services;

namespace Kinovea.ScreenManager
{
    public partial class ThumbnailViewerCameras : UserControl
    {
        #region Events
        public event EventHandler<CameraLoadAskedEventArgs> CameraLoadAsked;
        public event ProgressChangedEventHandler ProgressChanged;
        public event EventHandler BeforeLoad;
        public event EventHandler AfterLoad;
        #endregion
        
        #region Members
        private ThumbnailCamera selectedThumbnail;
        private int columns = (int)ExplorerThumbSize.Large;
        private List<ThumbnailCamera> thumbnailControls = new List<ThumbnailCamera>();
        private int imageReceived;
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        #endregion
        
        public ThumbnailViewerCameras()
        {
            InitializeComponent();
            //RefreshUICulture();
            this.Dock = DockStyle.Fill;
        }
        
        #region Public methods
        public void CamerasDiscovered(List<CameraSummary> summaries)
        {
            bool updated = UpdateThumbnailList(summaries);
            if(updated)
                DoLayout();
        }
        public void CameraImageReceived(CameraSummary summary, Bitmap image)
        {
            // This generally runs in a worker thread in the camera manager plug-in.
            if(this.InvokeRequired)
                this.BeginInvoke((Action) delegate {UpdateThumbnailImage(summary, image);});
            else
                UpdateThumbnailImage(summary, image);
        }
        public bool OnKeyPress(Keys keyCode)
        {
            bool handled = false;
            
            if(selectedThumbnail == null)
            {
                if(thumbnailControls.Count > 0)
                    thumbnailControls[0].SetSelected();
                
                return true;
            }
            
            int index = IndexOf(selectedThumbnail.Summary.Identifier);
            int row = index / columns;
            int col = index - (row * columns);
            
            switch (keyCode)
            {
                case Keys.Left:
                {
                    if(col > 0)
                        thumbnailControls[index - 1].SetSelected();
                    handled = true;
                    break;
                }
                case Keys.Right:
                {
                    if (col < columns - 1 && index + 1 < thumbnailControls.Count)
                        thumbnailControls[index + 1].SetSelected();
                    handled = true;
                    break;
                }
                case Keys.Up:
                {
                    if (row > 0)
                        thumbnailControls[index - columns].SetSelected();
                    this.ScrollControlIntoView(selectedThumbnail);
                    handled = true;
                    break;
                }
                case Keys.Down:
                {
                    if (index + columns  < thumbnailControls.Count)
                        thumbnailControls[index + columns].SetSelected();
                    this.ScrollControlIntoView(selectedThumbnail);
                    handled = true;
                    break;
                }
                case Keys.Return:
                {
                    if (CameraLoadAsked != null)
                        CameraLoadAsked(this, new CameraLoadAskedEventArgs(selectedThumbnail.Summary, -1));
                    handled = true;
                    break;
                }   
                case Keys.F2:
                {
                    // TODO: launch the alias dialog.
                    handled = true;
                    break;
                }
                default:
                    break;
            }
            
            return handled;
        }
        
        public void UpdateThumbnailsSize(ExplorerThumbSize newSize)
        {
            this.columns = (int)newSize;
            if(thumbnailControls.Count > 0)
                DoLayout();
        }
        #endregion

        #region Private methods
        private bool UpdateThumbnailList(List<CameraSummary> summaries)
        {
            bool updated = false;
            
            if(summaries.Count == 0)
            {
                if(BeforeLoad != null)
                    BeforeLoad(this, EventArgs.Empty);
                    
                imageReceived = 0;
            }
            
            // Add new cameras.
            List<string> found = new List<string>();
            foreach(CameraSummary summary in summaries)
            {
                found.Add(summary.Identifier);
                
                int index = IndexOf(summary.Identifier);
                
                if(index >= 0)
                    continue;
                
                updated = true;
                
                ThumbnailCamera thumbnail = new ThumbnailCamera(summary);
                thumbnail.LaunchCamera += Thumbnail_LaunchCamera;
				thumbnail.CameraSelected += Thumbnail_CameraSelected;
				thumbnail.SummaryUpdated += Thumbnail_SummaryUpdated;
				
                thumbnailControls.Add(thumbnail);
                this.Controls.Add(thumbnail);
                
                summary.Manager.GetSingleImage(summary);
            }
            
            // Remove cameras that were disconnected.
            List<ThumbnailCamera> lost = new List<ThumbnailCamera>();
            foreach(ThumbnailCamera thumbnail in thumbnailControls)
            {
                if(!found.Contains(thumbnail.Summary.Identifier))
                    lost.Add(thumbnail);
            }
            
            if(lost.Count > 0)
                updated = true;
                
            foreach(ThumbnailCamera thumbnail in lost)
            {
                this.Controls.Remove(thumbnail);
                thumbnailControls.Remove(thumbnail);
            }
            
            return updated;
        }
        
        private int IndexOf(string identifier)
        {
            for(int i = 0; i<thumbnailControls.Count; i++)
                if(thumbnailControls[i].Summary.Identifier == identifier)
                    return i;
            
            return -1;
        }
        
        private void DoLayout()
        {
            int leftMargin = 30;
            int rightMargin = 20;
            int topMargin = 5;
            
            int colWidth = (this.Width - leftMargin - rightMargin) / columns;
            int spacing = colWidth / 20;
            
            int thumbWidth = colWidth - spacing;
            int thumbHeight = (thumbWidth * 3 / 4) + 15;
            
            int current = 0;
            
            this.SuspendLayout();
            foreach(ThumbnailCamera thumbnail in thumbnailControls)
            {
                thumbnail.SetSize(thumbWidth, thumbHeight);

                int row = current / columns;
                int col = current - (row * columns);
                int left = col * colWidth + leftMargin;
                int top = topMargin + (row * (thumbHeight + spacing));
                thumbnail.Location = new Point(left, top);
                current++;
            }
            
            this.ResumeLayout();
        }
        
        private void UpdateThumbnailImage(CameraSummary summary, Bitmap image)
        {
            if(summary == null)
                return;
            
            int index = IndexOf(summary.Identifier);
            if(index < 0)
                return;
                
            bool hasImage = thumbnailControls[index].Image != null;
            thumbnailControls[index].UpdateImage(image);
            
            if(hasImage)
                return;
            
            imageReceived++;
            int percentage = (int)(((float)imageReceived / thumbnailControls.Count) * 100);
            
            if(ProgressChanged != null)
                ProgressChanged(this, new ProgressChangedEventArgs(percentage, null));
            
            if(imageReceived >= thumbnailControls.Count && AfterLoad != null)
                AfterLoad(this, EventArgs.Empty);
        }
        
        private void ThumbnailViewerCameras_Resize(object sender, EventArgs e)
        {
            DoLayout();
        }
        
        private void Thumbnail_LaunchCamera(object sender, EventArgs e)
        {
            ThumbnailCamera thumbnail = sender as ThumbnailCamera;
        
            if (thumbnail != null && CameraLoadAsked != null)
                CameraLoadAsked(this, new CameraLoadAskedEventArgs(thumbnail.Summary, -1));
        }
        private void Thumbnail_CameraSelected(object sender, EventArgs e)
        {
            ThumbnailCamera thumbnail = sender as ThumbnailCamera;
        
            if(thumbnail == null)
                return;
                
            if (selectedThumbnail != null && selectedThumbnail != thumbnail )
                selectedThumbnail.SetUnselected();
        
            selectedThumbnail = thumbnail;
        }
        private void Thumbnail_SummaryUpdated(object sender, EventArgs e)
        {
            ThumbnailCamera thumbnail = sender as ThumbnailCamera;
            CameraTypeManager.UpdatedCameraSummary(thumbnail.Summary);
        }
        #endregion
    }
}