using System;
using System.Diagnostics;
using System.Windows.Media;
using Nextended.Core;

namespace Nextended.UI.ViewModels
{
	/// <summary>
	/// Model für Filter
	/// </summary>
	[DebuggerDisplay("{Caption}")]
	public class ItemFilterModel : NotificationObject
	{
		private string caption;
		private string description;
		private ImageSource image;

		/// <summary>
		/// FinancialItemFilterModel
		/// </summary>

		public ItemFilterModel(string caption, 
		                       string description = "", ImageSource image = null)
		{
			this.caption = caption;
			this.description = description;
			this.image = image;
		}

		/// <summary>
		/// Image
		/// </summary>
		public ImageSource Image
		{
			get { return image; }
			set { SetProperty(ref image, value, () => Image); }
		}

		/// <summary>
		/// Description
		/// </summary>
		public string Description
		{
			get { return description; }
			set { SetProperty(ref description, value, () => Description); }
		}

		/// <summary>
		/// Caption
		/// </summary>
		public string Caption
		{
			get { return caption; }
			set { SetProperty(ref caption, value, () => Caption); }
		}
	}

    /// <summary>
    /// Model für ItemFilter eines bestimmten
    /// Item-Typs.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ItemFilterModel<T> : ItemFilterModel
    {
        private Func<T, bool> expression;

        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="caption"></param>
        /// <param name="description"></param>
        /// <param name="image"></param>
        public ItemFilterModel(Func<T, bool> expression, string caption,
                           string description = "", ImageSource image = null) : base(caption, description, image)
        {
            this.expression = expression;
        }

        /// <summary>
        /// Expression
        /// </summary>
        public Func<T, bool> Expression
        {
            get { return expression; }
            set { SetProperty(ref expression, value, () => Expression); }
        }
    }
}