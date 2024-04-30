using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace WYW.RS232SOCKET.Models
{
    internal class LineGraphModel: ObservableObject
    {

        public LineGraphModel(string description, Brush stroke)
        {
           Description= description;
            Stroke= stroke;
        }

        #region  属性
        private PointCollection points=new PointCollection();
        private Brush stroke;

        public string Description { get; set; }
        public float StrokeThickness { get; set; } = 2;
        /// <summary>
        /// 
        /// </summary>
        public PointCollection Points
        {
            get => points;
            set => SetProperty(ref points, value);
        }
        /// <summary>
        /// 
        /// </summary>
        public Brush Stroke
        {
            get => stroke;
            set => SetProperty(ref stroke, value);
        }
        #endregion

    }
}
