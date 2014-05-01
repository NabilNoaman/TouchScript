/*
 * @author Valentin Simonov / http://va.lent.in/
 */

using System.Collections.Generic;
using TUIOsharp;
using TUIOsharp.Entities;
using UnityEngine;

namespace TouchScript.InputSources
{
    /// <summary>
    /// Processes TUIO 1.0 input.
    /// </summary>
    [AddComponentMenu("TouchScript/Input Sources/TUIO Input")]
    public sealed class TuioInput : InputSourcePro
    {
        #region Unity fields

        /// <summary>
        /// Port to listen to.
        /// </summary>
        public int TuioPort = 3333;

        #endregion

        #region Private variables

        private TuioServer server;
        private Dictionary<TuioCursor, int> cursorToInternalId = new Dictionary<TuioCursor, int>();
        private Dictionary<TuioBlob, int> blobToInternalId = new Dictionary<TuioBlob, int>();
        private Dictionary<TuioObject, int> objectToInternalId = new Dictionary<TuioObject, int>();
        private int screenWidth;
        private int screenHeight;

        #endregion

        #region Unity

        /// <inheritdoc />
        protected override void OnEnable()
        {
            base.OnEnable();

            server = new TuioServer(TuioPort);
            server.CursorAdded += tuioCursorAddedHandler;
            server.CursorUpdated += tuioCursorUpdatedHandler;
            server.CursorRemoved += tuioCursorRemovedHandler;
            server.BlobAdded += tuioBlobAddedHandler;
            server.BlobUpdated += tuioBlobUpdatedHandler;
            server.BlobRemoved += tuioBlobRemovedHandler;
            server.ObjectAdded += tuioObjectAddedHandler;
            server.ObjectUpdated += tuioObjectUpdatedHandler;
            server.ObjectRemoved += tuioObjectRemovedHandler;
            server.Connect();
        }

        /// <inheritdoc />
        protected override void Update()
        {
            base.Update();
            screenWidth = Screen.width;
            screenHeight = Screen.height;
        }

        /// <inheritdoc />
        protected override void OnDisable()
        {
            if (server != null)
            {
                server.CursorAdded -= tuioCursorAddedHandler;
                server.CursorUpdated -= tuioCursorUpdatedHandler;
                server.CursorRemoved -= tuioCursorRemovedHandler;
                server.BlobAdded -= tuioBlobAddedHandler;
                server.BlobUpdated -= tuioBlobUpdatedHandler;
                server.BlobRemoved -= tuioBlobRemovedHandler;
                server.ObjectAdded -= tuioObjectAddedHandler;
                server.ObjectUpdated -= tuioObjectUpdatedHandler;
                server.ObjectRemoved -= tuioObjectRemovedHandler;
                server.Disconnect();
            }

            foreach (var i in cursorToInternalId)
            {
                cancelTouch(i.Value);
            }

            base.OnDisable();
        }

        #endregion

        #region Private functions

        private Vector2 convertScreenPosition(TuioEntity entity)
        {
            return new Vector2(entity.X * screenWidth, (1 - entity.Y) * screenHeight);
        }

        #endregion

        #region Event handlers

        private void tuioCursorAddedHandler(object sender, TuioCursorEventArgs tuioCursorEventArgs)
        {
            var cursor = tuioCursorEventArgs.Cursor;
            lock (cursorToInternalId)
            {
                Debug.Log(string.Format("Cursor added ({0} {1} {2} {3} {4} {5})", cursor.Id, cursor.X, cursor.Y, cursor.VelocityX, cursor.VelocityY, cursor.Acceleration));
                cursorToInternalId.Add(cursor, beginTouch(convertScreenPosition(cursor)));
            }
        }

        private void tuioCursorUpdatedHandler(object sender, TuioCursorEventArgs tuioCursorEventArgs)
        {
            var cursor = tuioCursorEventArgs.Cursor;
            lock (cursorToInternalId)
            {
                int existingCursor;
                if (!cursorToInternalId.TryGetValue(cursor, out existingCursor)) return;

                Debug.Log(string.Format("Cursor updated ({0} {1} {2} {3} {4} {5})", cursor.Id, cursor.X, cursor.Y, cursor.VelocityX, cursor.VelocityY, cursor.Acceleration));
                moveTouch(existingCursor, convertScreenPosition(cursor));
            }
        }

        private void tuioCursorRemovedHandler(object sender, TuioCursorEventArgs tuioCursorEventArgs)
        {
            var cursor = tuioCursorEventArgs.Cursor;
            lock (cursorToInternalId)
            {
                int existingCursor;
                if (!cursorToInternalId.TryGetValue(cursor, out existingCursor)) return;

                Debug.Log(string.Format("Cursor Removed ({0})", cursor.Id));
                cursorToInternalId.Remove(cursor);
                endTouch(existingCursor);
            }
        }

        private void tuioBlobAddedHandler(object sender, TuioBlobEventArgs tuioBlobEventArgs)
        {
            var blob = tuioBlobEventArgs.Blob;
            lock (blobToInternalId)
            {
                Debug.Log(string.Format("Blob added ({0} {1} {2} {3} {4} {5} {6} {7} {8} {9} {10} {11})", blob.Id, blob.X, blob.Y, blob.Angle, blob.Width, blob.Height, blob.Area, blob.VelocityX, blob.VelocityY, blob.RotationVelocity, blob.Acceleration, blob.RotationAcceleration));
                blobToInternalId.Add(blob, beginTouch(convertScreenPosition(blob)));
            }
        }

        private void tuioBlobUpdatedHandler(object sender, TuioBlobEventArgs tuioBlobEventArgs)
        {
            var blob = tuioBlobEventArgs.Blob;
            lock (blobToInternalId)
            {
                int existingBlob;
                if (!blobToInternalId.TryGetValue(blob, out existingBlob)) return;

                Debug.Log(string.Format("Blob updated ({0} {1} {2} {3} {4} {5} {6} {7} {8} {9} {10} {11})", blob.Id, blob.X, blob.Y, blob.Angle, blob.Width, blob.Height, blob.Area, blob.VelocityX, blob.VelocityY, blob.RotationVelocity, blob.Acceleration, blob.RotationAcceleration));
                moveTouch(existingBlob, convertScreenPosition(blob));
            }
        }

        private void tuioBlobRemovedHandler(object sender, TuioBlobEventArgs tuioBlobEventArgs)
        {
            var blob = tuioBlobEventArgs.Blob;
            lock (blobToInternalId)
            {
                int existingBlob;
                if (!blobToInternalId.TryGetValue(blob, out existingBlob)) return;

                Debug.Log(string.Format("Blob removed ({0})", blob.Id));
                blobToInternalId.Remove(blob);
                endTouch(existingBlob);
            }
        }

        private void tuioObjectAddedHandler(object sender, TuioObjectEventArgs tuioObjectEventArgs)
        {
            var obj = tuioObjectEventArgs.Object;
            lock (objectToInternalId)
            {
                Debug.Log(string.Format("Object added ({0} {1} {2} {3} {4} {5} {6} {7} {8} {9})", obj.Id, obj.ClassId, obj.X, obj.Y, obj.Angle, obj.VelocityX, obj.VelocityY, obj.RotationVelocity, obj.Acceleration, obj.RotationAcceleration));
                objectToInternalId.Add(obj, beginTouch(convertScreenPosition(obj)));
            }
        }

        private void tuioObjectUpdatedHandler(object sender, TuioObjectEventArgs tuioObjectEventArgs)
        {
            var obj = tuioObjectEventArgs.Object;
            lock (objectToInternalId)
            {
                int existingObject;
                if (!objectToInternalId.TryGetValue(obj, out existingObject)) return;

                Debug.Log(string.Format("Object updated ({0} {1} {2} {3} {4} {5} {6} {7} {8} {9})", obj.Id, obj.ClassId, obj.X, obj.Y, obj.Angle, obj.VelocityX, obj.VelocityY, obj.RotationVelocity, obj.Acceleration, obj.RotationAcceleration));
                moveTouch(existingObject, convertScreenPosition(obj));
            }
        }

        private void tuioObjectRemovedHandler(object sender, TuioObjectEventArgs tuioObjectEventArgs)
        {
            var obj = tuioObjectEventArgs.Object;
            lock (objectToInternalId)
            {
                int existingObject;
                if (!objectToInternalId.TryGetValue(obj, out existingObject)) return;

                Debug.Log(string.Format("Object removed ({0})", obj.Id));
                objectToInternalId.Remove(obj);
                endTouch(existingObject);
            }
        }

        #endregion
    }
}