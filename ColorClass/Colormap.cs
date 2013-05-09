// Programmed by kaoruzen.
//    http://kzen.my-sv.net/
// 
// 2010/02/01 作成
/**************************
 * Katsuhiro Morishita 追記
 * 
 * 2012/4/30    コードを一部改編した。
 * 2012/8/27    getColor()のバグを修正
 *              エラーをスローする場合にメッセージを残すように変更
 *              関数の頭文字を大文字へ変更
 *              Convert.Int16()を(int)へ換装
 * ************************/

using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;


namespace Graphic.ColorClass
{
    /// <summary>
    /// 2次元グラフ用のカラーマップを生成します。
    /// </summary>
    public class Colormap
    {
        /// <summary>
        /// HSV色環の開始H値[deg]
        /// </summary>
        private double StartH;

        /// <summary>
        /// HSV色環の終了H値[deg]
        /// </summary>
        private double EndH;

        /// <summary>
        /// HSV色環のthis.S値
        /// </summary>
        private double S;

        /// <summary>
        /// HSV色環のthis.V値
        /// </summary>
        private double V;

        /// <summary>
        /// 逆順描画
        /// </summary>
        private bool Reverse;


        /// <summary>
        /// 青から始まり赤で終わる、標準的なカラーマップを生成
        /// </summary>
        public Colormap()
        {
            GenerateColorMap(240.0, 0, 100.0, 100.0);
        }

        /// <summary>
        /// 開始色と終了色を指定したカラーマップを生成
        /// </summary>
        /// <param name="start">0%となる色の色相(degree 0～360)</param>
        /// <param name="end">100%となる色の色相(degree 0～360)</param>
        public Colormap(double start, double end)
        {
            GenerateColorMap(start, end, 100.0, 100.0);
        }

        /// <summary>
        /// 開始色と終了色、明度と彩度を指定したカラーマップを生成
        /// </summary>
        /// <param name="start">0%となる色の色相(degree 0～360)</param>
        /// <param name="end">100%となる色の色相(degree 0～360)</param>
        /// <param name="saturation">彩度(0～100%)</param>
        /// <param name="brightness">明度(0～100%)</param>
        public Colormap(double start, double end, double saturation, double brightness)
        {
            GenerateColorMap(start, end, saturation, brightness);
        }

        /// <summary>
        /// カラーマップ用の各種変数の初期化
        /// （コンストラクタから呼び出される）
        /// </summary>
        /// <param name="start">0%となる色の色相(degree 0～360)</param>
        /// <param name="end">100%となる色の色相(degree 0～360)</param>
        /// <param name="saturation">彩度(0～100%)</param>
        /// <param name="brightness">明度(0～100%)</param>
        private void GenerateColorMap(double start, double end, double saturation, double brightness)
        {
            if (start > end)
            {    
                this.Reverse = true;
                this.StartH = end % 360;
                this.EndH = start % 360;
            }
            else
            {
                this.Reverse = false;
                this.StartH = start % 360;
                this.EndH = end % 360;
            }

            if (saturation > 100) { this.S = 100; }
            else if (saturation < 0) { this.S = 0; }
            else { this.S = saturation; }

            if (brightness > 100) { this.V = 100; }
            else if (brightness < 0) { this.V = 0; }
            else { this.V = brightness; }
        }

        /// <summary>
        /// 与えられた値の色を返す
        /// </summary>
        /// <param name="value">値（最小値≦値≦最大値）</param>
        /// <param name="min">最小値</param>
        /// <param name="max">最大値</param>
        /// <returns>Colorオブジェクト</returns>
        public Color GetColor(double value, double min, double max)
        {
            if (value < min || value > max || min == max)
            {
                throw new ArgumentOutOfRangeException("ColormapクラスのgetColor()にてエラーがスローされました。value, min, maxの値の範囲が不正です。");
            }
            else
            {
                return GetColor((value - min) / (max - min));
            }
        }

        /// <summary>
        /// 与えられた値の色を返す
        /// </summary>
        /// <param name="value">値（0.0～1.0）</param>
        /// <returns>Colorオブジェクト</returns>
        public Color GetColor(double value)
        {
            if (value > 1.0 || value < 0.0)
            {
                throw new ArgumentOutOfRangeException("ColormapクラスのgetColor()にてエラーがスローされました。valueの値の範囲が不正です。");
            }
            else
            {
                if (this.Reverse == true)
                {
                    value = 1.0 - value;
                }
                return ConvertHSVtoRGB(((this.EndH - this.StartH) * value) + this.StartH, this.S, this.V);
            }
        }

        /// <summary>
        /// HSV色空間情報からColorオブジェクトへ変換します。
        /// 参考： http://ja.wikipedia.org/wiki/HSV色空間
        /// </summary>
        /// <param name="_h">色相(0～360)</param>
        /// <param name="_s">彩度(0～100)</param>
        /// <param name="_v">明度(0～100)</param>
        /// <returns>HSV色空間データから生成されたColorオブジェクト</returns>
        private Color ConvertHSVtoRGB(double _h, double _s, double _v)
        {
            Color tempColor = new Color();

            _s = _s / 100;
            _v = _v / 100;
            _h = _h % 360;

            if (_s == 0.0)      //Gray
            {
                int rgb = (int)((double)(_v * 255));

                tempColor = Color.FromArgb(rgb, rgb, rgb);
            }
            else
            {
                int Hi = (int)((_h / 60.0) % 6);
                double f = (_h / 60.0) - Hi;

                double p = _v * ( 1 - _s);
                double q = _v * (1 - f * _s);
                double t = _v * (1 - (1 - f) * _s);

                double r = 0.0;
                double g = 0.0;
                double b = 0.0;

                switch (Hi)
                {
                    case 0:
                        r = _v;
                        g = t;
                        b = p;
                        break;

                    case 1:
                        r = q;
                        g = _v;
                        b = p;
                        break;

                    case 2:
                        r = p;
                        g = _v;
                        b = t;
                        break;

                    case 3:
                        r = p;
                        g = q;
                        b = _v;
                        break;

                    case 4:
                        r = t;
                        g = p;
                        b = _v;
                        break;

                    case 5:
                        r = _v;
                        g = p;
                        b = q;
                        break;

                    default:
                        r = 0;
                        g = 0;
                        b = 0;
                        break;
                }

                tempColor = Color.FromArgb(
                    (int)(r * 255.0),
                    (int)(g * 255.0),
                    (int)(b * 255.0));
            }
            return tempColor;
        }
    }
}
