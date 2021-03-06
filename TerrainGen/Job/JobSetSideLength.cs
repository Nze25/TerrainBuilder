﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerrainGen.Job
{
    class JobSetSideLength : IJob
    {
        private readonly int _sideLength;

        public JobSetSideLength(int sideLength)
        {
            _sideLength = sideLength;
        }

        /// <inheritdoc />
        public void Execute(RenderManager renderManager)
        {
            renderManager.SideLength = _sideLength;
            renderManager.CreateChunks();
        }

        /// <inheritdoc />
        public bool CanExecuteInBackground()
        {
            return true;
        }

        public bool IsCancellable()
        {
            return false;
        }
    }
}
