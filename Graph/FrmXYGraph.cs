/******************************************
 * グラフ表示名前空間
 * 　グラフ関係のクラスを格納します。
 * [開発者]
 *  森下功啓　熊本大学
 * [履歴] 
 *  2011/ 7/24  開発開始
 *  2012/ 1/ 5  データの受け渡しで余計な時間を食わない様に、Taskクラスを使った処理を導入した。
 *              まだ記述方法に慣れていないので冗長だが…処理にかかる時間は確実に短くなった。
 * ****************************************/
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

using Graphic.ColorClass;

namespace Graphic.Graph
{
    /// <summary>
    /// グラフフォームクラスです。
    /// 基本、散布図を表示することを想定しています。
    /// 2011/7/24 やはり突貫で作ったので洗練されていません。
    /// </summary>
    public partial class FrmXYGraph : Form
    {
        /*-------------- 列挙体宣言 ----------------*/
        /// <summary>
        /// 描画モード
        /// </summary>
        public enum PlotKind 
        {
            /// <summary>点線モード</summary>
            dot,
            /// <summary>実線モード</summary>
            line
        }
        /*-------------- 構造体宣言 ----------------*/
        /// <summary>
        /// x,yの座標セット
        /// マップのアドレス計算用
        /// </summary>
        private struct Pos
        {
            public int x;
            public int y;
            //二項+演算子をオーバーロードする（これで足し算が簡単にできる）
            public static Pos operator +(Pos c1, Pos c2)
            {
                return new Pos(c1.x + c2.x, c1.y + c2.y);
            }
            //二項-演算子をオーバーロードする（これで足し算が簡単にできる）
            public static Pos operator -(Pos c1, Pos c2)
            {
                return new Pos(c1.x - c2.x, c1.y - c2.y);
            }
            public Pos(int x0 = 0, int y0 = 0) { this.x = x0; this.y = y0; }    // 初期値セット機能付きコンストラクタのオーバーロード
        }
        /// <summary>
        /// x,yの組を格納する構造体です
        /// </summary>
        public struct Position 
        {
            /// <summary>x座標値</summary>
            public double x;
            /// <summary>y座標値</summary>
            public double y;
            /// <summary>
            /// コンストラクタ
            /// </summary>
            /// <param name="_x">x座標値</param>
            /// <param name="_y">y座標値</param>
            public Position(double _x, double _y) { this.x = _x; this.y = _y; }
        }
        /// <summary>
        /// グラフの右上と左下を格納する構造体です
        /// <para>グラフのマッピングに使用します。</para>
        /// </summary>
        public struct Corner
        {
            /* メンバ変数 *************************************/
            /// <summary>右上の座標</summary>
            public readonly Position upperRight;
            /// <summary>左下の座標</summary>
            public readonly Position lowerLeft;
            /* プロパティ *************************************/
            /// <summary>
            /// プロットエリアの軸比
            /// <para>y軸/x軸で定義しています。</para>
            /// </summary>
            public double Ratio { get { return Math.Abs((this.upperRight.y - this.lowerLeft.y) / (this.upperRight.x - this.lowerLeft.x)); } }
            /* メソッド ***************************************/
            /// <summary>
            /// 座標格納用
            /// </summary>
            /// <param name="_upRight">右上の座標</param>
            /// <param name="_lowLeft">左下の座標<para>通常は原点です。</para></param>
            public Corner(Position _upRight, Position _lowLeft) { this.upperRight = _upRight; this.lowerLeft = _lowLeft; }
        }
        /*-------------- グローバル変数宣言 ----------------*/
        /// <summary>グラフの座標</summary>
        private Corner _corner;
        /// <summary>描画モード</summary>
        private PlotKind _plotKind;
        /// <summary>表示済みだとtrue</summary>
        private Boolean plotDone = true; 
        /// <summary>データを格納する配列</summary>
        private double[] x, y;
        /// <summary>描写するデータ量</summary>
        private int length;
        /// <summary>全体のデータ量</summary>
        private int lengthOverAll;
        /// <summary>プロットデータの軸比<para>x軸成分1つ当たりのy軸成分1つの大きさと定義する。</para><para>ratio = Height / Width</para></summary>
        private double axisRatio;
        /// <summary>表示処理中にデータが変更されるのを伏せぶためのロックフラグ</summary>
        private Boolean plotLock = false;
        /// <summary>データの取得に使用するタスク</summary>
        Task task;
        /// <summary>背景画像</summary>
        Image _bmp;
        /*-------------- メソッド宣言 ----------------*/
        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="kind">グラフの描画モードdot/line</param>
        /// <param name="graphName">グラフの名前</param>
        public FrmXYGraph(PlotKind kind, string graphName = "MyGraph")
        {
            if (System.IO.File.Exists("map01.jpg"))this._bmp = Image.FromFile("map01.jpg");
            this.Text = graphName;
            this._plotKind = kind;
            InitializeComponent();
        }
        /// <summary>
        /// フォームを表示する
        /// </summary>
        /// <param name="_corner">コーナーの座標</param>
        /// <param name="_axisRatio">プロットデータの軸比<para>x軸成分1つ当たりのy軸成分1つの大きさと定義する。</para><para>ratio = Height / Width</para></param>
        public void Show(Corner _corner, double _axisRatio)
        {
            this._corner = _corner;
            this.axisRatio = _axisRatio;
            this.ClientSize = new Size(this.ClientSize.Width, (int)(this.axisRatio * this._corner.Ratio * (double)this.ClientSize.Width));  // 画面の縮尺を調整
            this.Show();
        }
        /// <summary>
        /// 対角の座標と軸比をセットします
        /// </summary>
        /// <param name="_corner">コーナーの座標</param>
        /// <param name="_axisRatio">プロットデータの軸比<para>x軸成分1つ当たりのy軸成分1つの大きさと定義する。</para><para>ratio = Height / Width</para></param>
        public void SetCorner(Corner _corner, double _axisRatio)
        {
            this._corner = _corner;
            this.axisRatio = _axisRatio;
        }
        /// <summary>
        /// 指定座標のx,yアドレスを返す
        /// <para>
        /// 境界値と同じ座標を渡すと、計算アドレスがマップサイズと等しくなり、存在しないアドレスを指すことになることに注意すること。
        /// つまり、マップの要素番号は0～size-1であることに注意せよってこと。
        /// また、マップ領域外の座標を指定されてもマップのローカル座標系のアドレスを返す。
        /// </para>
        /// </summary>
        /// <param name="_position">指定座標（緯度・経度）</param>
        /// <returns>x,yアドレス</returns>
        private Pos Position2XYaddress(Position _position)
        {
            Pos ans = new Pos();

            ans.y = (int)((double)this.ClientSize.Height / (this._corner.lowerLeft.y - this._corner.upperRight.y) * (_position.y - this._corner.upperRight.y));       // 将来、C#のキャストの仕様が変更になったらバグとなるので注意(2011/7)
            ans.x = (int)((double)this.ClientSize.Width / (this._corner.upperRight.x - this._corner.lowerLeft.x) * (_position.x - this._corner.lowerLeft.x));
            return ans;
        }
        /// <summary>
        /// グラフ表示を指示するメソッド
        /// 散布図形式となっている。
        /// ただちに表示されるわけではない。
        /// 
        /// 要素数を指定することで、指定配列の特定要素までの描写を行うということが可能になる。
        /// </summary>
        /// <param name="_x">x軸成分の配列</param>
        /// <param name="_y">y軸成分の配列</param>
        /// <param name="_length">配列の要素数</param>
        public void Plot(double[] _x, double[] _y, int _length)
        {
            if (this.plotLock == false)
            {
                this.x = new double[_length];
                this.y = new double[_length];
                this.length = _length;
                this.lengthOverAll = _length;
                Parallel.For(0, _length, i =>
                {
                    this.x[i] = _x[i];
                    this.y[i] = _y[i];
                });
                this.plotDone = false;
                this.timer4Calc.Enabled = true;
                //this.Refresh();
                //this.GraphPaint();
            }
            return;
        }
        /// <summary>
        /// グラフ表示を指示するメソッドその2
        /// <para>
        /// 散布図形式となっている。
        /// ただちに表示されるわけではない。
        /// 
        /// 全体のデータ量を指定できるので、描写における色と時間や順番を関連付けられる。
        /// </para>
        /// <para>[2012/1/5追記]Taskクラスの使い方がまだ不慣れでうまく記述できない。。。</para>
        /// </summary>
        /// <param name="_x">x軸成分の配列</param>
        /// <param name="_y">y軸成分の配列</param>
        /// <param name="_length">配列の要素数</param>
        /// <param name="_lenghtOverAll">全体のデータ数</param>
        public void Plot(double[] _x, double[] _y, int _length, int _lenghtOverAll)
        {
            
            if (this.plotLock == false)
            {
                if (this.task == null)
                {
                    this.task = Task.Factory.StartNew(() => // タスクを立ち上げて、データを読み込む
                    {
                        this.Plot(_x, _y, _length);
                        this.lengthOverAll = _lenghtOverAll;
                    });
                }
                else
                {
                    if (task.IsCompleted) 
                    {
                        this.task = Task.Factory.StartNew(() => // タスクを立ち上げて、データを読み込む
                        {
                            this.Plot(_x, _y, _length);
                            this.lengthOverAll = _lenghtOverAll;
                        });
                    }
                }
            }
            /*
            if (this.plotLock == false) // データコピーを終えるまで待つ処理。呼び出し側では、上記のTaskの10倍ほど時間がかかる。
            {
                this.Plot(_x, _y, _length);
                this.lengthOverAll = _lenghtOverAll;
            }
            */
            return;
        }
        /// <summary>
        /// 「閉じるボタン」を無効化する
        /// </summary>
        protected override CreateParams CreateParams
        {
            [System.Security.Permissions.SecurityPermission(
                System.Security.Permissions.SecurityAction.LinkDemand,
                Flags = System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
            get
            {
                const int CS_NOCLOSE = 0x200;
                CreateParams cp = base.CreateParams;
                cp.ClassStyle = cp.ClassStyle | CS_NOCLOSE;
                return cp;
            }
        }
        
        /// <summary>
        /// タイマーの動作
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timer4Calc_Tick(object sender, EventArgs e)
        {
            if (this.plotDone == false) this.GraphPaint();  // データを未描写であれば描写する
        }
        /// <summary>
        /// 別スレッドからアクセスする際に使用するデリゲート
        /// </summary>
        delegate void GraphPaintDelegate();
        /// <summary>
        /// グラフを表示する本体の関数
        /// </summary>
        private void GraphPaint()
        {
            Colormap myColorMap = new Colormap();   // 色空間クラス
            Graphics g = this.pictureBox.CreateGraphics();

            this.plotLock = true;                   // データの書き換えが途中で行われないようにするためにロック
            if (this.InvokeRequired)
            {
                GraphPaintDelegate a = new GraphPaintDelegate(this.GraphPaint);
                this.Invoke(a);
            }
            else
            {
                this.ClientSize = new Size(this.ClientSize.Width, (int)(this.axisRatio * this._corner.Ratio * (double)this.ClientSize.Width));      // 画面の縮尺を調整
                this.pictureBox.Refresh();
                if (this.x.Length < length && this.y.Length < this.length) { MessageBox.Show("配列のサイズエラーです。"); return; }
                Pen myPen = new Pen(Color.FromArgb(255, Color.Red), 2);                             // ライン用のオブジェクト

                for (int i = 0; i < length; i++)
                {
                    Brush myBrush = new SolidBrush(myColorMap.GetColor((double)i / (double)this.lengthOverAll));  // 塗りつぶし用のオブジェクト 時間の経過を色で表現する
                    Pos add = this.Position2XYaddress(new Position(this.x[i], this.y[i]));          // 描画領域内のアドレスを計算するアドレスを格納する
                    if (add.x >= 0 && add.x < this.ClientSize.Width && add.y >= 0 && add.y < this.ClientSize.Height)
                    {
                        g.FillRectangle(myBrush, add.x, add.y, 4, 4);
                    }
                }
                this.plotDone = true;
            }
            g.Dispose();
            this.plotLock = false;                                                                  // ロック解除
            return;
        }
        private void FrmXYGraph_Paint(object sender, PaintEventArgs e)
        {
            
        }

        private void FrmXYGraph_MouseMove(object sender, MouseEventArgs e)
        {
            double x = (double)e.X / (double)this.ClientSize.Width * (this._corner.upperRight.x - this._corner.lowerLeft.x) + this._corner.lowerLeft.x;
            double y = -(double)e.Y / (double)this.ClientSize.Height * (this._corner.upperRight.y - this._corner.lowerLeft.y);
            this.PointLabel.Text = x.ToString("0.0") + ", " + y.ToString("0.0") ;
        }
    }
}
