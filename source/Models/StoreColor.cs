using System;
using System.Windows.Media;

namespace GameActivity.Models
{
    public class StoreColor
	{
		public Guid Id { get; set; }
		public string Name { get; set; }
        public Brush Fill { get; set; }
    }
}