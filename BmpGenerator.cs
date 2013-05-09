/****************************************************
 * BmpGenerator.cs
 * Colorクラスオブジェクトの2元配列から画像を生成するクラス
 * 
 * [メモ]
 *          将来、他の2次元データから画像を作れるようにするかもしれない。
 * 
 * [開発履歴]
 *          2012/5/29   この頃msdnを参考に本クラスを作成した。
 * **************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using System.Threading.Tasks;

namespace Graphic
{
    /// <summary>
    /// Colorクラスオブジェクトの2元配列から画像を生成するクラス
    /// </summary>
    public class BmpGenerator
    {
        /*  ********************************************/
        /// <summary>
        /// 画素値を格納する構造体
        /// </summary>
        public struct PixelData
        {
            /// <summary>
            /// 青
            /// </summary>
            public byte blue;
            /// <summary>
            /// 緑
            /// </summary>
            public byte green;
            /// <summary>
            /// 赤
            /// </summary>
            public byte red;
        }
        /*  ********************************************/
        /// <summary>
        /// ビットマップデータ
        /// </summary>
        private System.Drawing.Bitmap bitmap;
        /*  ********************************************/
        /// <summary>
        /// 生成したビットマップデータ
        /// <para>画像オブジェクトのインスタンスを新規に作成して渡します。</para>
        /// </summary>
        public System.Drawing.Bitmap Bitmap
        {
            get
            {
                return new System.Drawing.Bitmap(this.bitmap); ;
            }
        }
        /// <summary>
        /// 画像サイズ
        /// </summary>
        public Point PixelSize
        {
            get
            {
                if (this.bitmap != null)
                {
                    GraphicsUnit unit = GraphicsUnit.Pixel;
                    RectangleF bounds = this.bitmap.GetBounds(ref unit);
                    return new Point((int)bounds.Width, (int)bounds.Height);
                }
                else
                    return new Point();
            }
        }
        /*  ********************************************/
        /// <summary>
        /// 破棄メソッド
        /// </summary>
        public void Dispose()
        {
            this.bitmap.Dispose();
        }
        /// <summary>
        /// 画像情報とアドレスからポインタを計算して返す
        /// </summary>
        /// <param name="bmpData">画像情報</param>
        /// <param name="x">xアドレス</param>
        /// <param name="y">yアドレス</param>
        /// <returns>ポインタ</returns>
        private unsafe PixelData* PixelAt(BitmapData bmpData, int x, int y)
        {
            return (PixelData*)(bmpData.Scan0 + y * bmpData.Stride + x * sizeof(PixelData));
        }
        /// <summary>
        /// 配列をセットすることで画像を生成する
        /// <para>生成された画像はBitmapプロパティにより取得可能です。</para>
        /// </summary>
        /// <param name="bmp">画像化したい2次元配列</param>
        public unsafe void MakeBmp(Color[,] bmp)
        {
            this.bitmap = new System.Drawing.Bitmap(bmp.GetLength(0), bmp.GetLength(1));    // インスタンス確保
            Point size = PixelSize;
            Rectangle rect = new Rectangle(0, 0, this.bitmap.Width, this.bitmap.Height);
            var bmpData = this.bitmap.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            //for (int y = 0; y < size.Y; y++)
            Parallel.For(0, size.Y, y =>
            {
                //for (int x = 0; x < size.X; x++)
                Parallel.For(0, size.X, x =>
                {
                    PixelData* pPixel = this.PixelAt(bmpData, x, y);
                    pPixel->red = bmp[x, y].R;
                    pPixel->green = bmp[x, y].G;
                    pPixel->blue = bmp[x, y].B;
                });
            });
            this.bitmap.UnlockBits(bmpData);
        }
        /// <summary>
        /// コンストラクタ
        /// </summary>
        public BmpGenerator()
        {
            this.bitmap = null;
        }
    }
}
