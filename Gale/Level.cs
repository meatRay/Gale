﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Box2DX.Common;
using OpenTK;
using Gale.Props;

namespace Gale
{
    class Level : IDisposable
    {
        public Sprite Backdrop;
        public Game Context { get; private set; }

        public PropManager Props { get; private set; }


        public Level(Game game_context)
        {
            Context = game_context;
            Props = new PropManager(this);
        }
        public void Render(Shader render_context)
        {
            Backdrop?.Render(render_context);
            // Order gets to be expensive over 10k props probably.  Find a cleaner solution!!
            foreach (var sprite in Props.All.OrderByDescending(p => p.GetPosition().Y + p.ZPosition))
                sprite.Render(render_context);
        }

        public void Update(double delta_time)
        {
            foreach (var prop in Props.All)
                prop.Update(delta_time);
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Backdrop.Dispose();
                    Props.Dispose();
                }
                disposedValue = true;
            }
        }
        public void Dispose() => Dispose(true);
        #endregion
    }
}
