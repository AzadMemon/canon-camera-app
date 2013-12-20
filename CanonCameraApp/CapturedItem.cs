using System;

namespace CanonCameraApp
{
    public class CapturedItem
    {
        private long _size;
        private String _name;
        private bool _isFolder;
        private byte[] _item;
        private int _height;
        private int _width;

        public long Size
        {
            get { return this._size; }
            set { this._size = value; }
        }

        public string Name
        {
            get { return this._name; }
            set { this._name = value; }
        }

        public bool IsFolder
        {
            get { return this._isFolder; }
            set { this._isFolder = value; }
        }

        public byte[] Item
        {
            get { return this._item; }
            set { this._item = value; }
        }

        public int Height
        {
            get { return this._height; }
            set { this._height = value; }
        }

        public int Width
        {
            get { return this._width; }
            set { this._width = value; }
        }
    }
}