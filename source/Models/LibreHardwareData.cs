using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameActivity.Models
{
	public class Child
	{
		[SerializationPropertyName("id")]
		public int Id { get; set; }

		[SerializationPropertyName("Text")]
		public string Text { get; set; }

		[SerializationPropertyName("Min")]
		public string Min { get; set; }

		[SerializationPropertyName("Value")]
		public string Value { get; set; }

		[SerializationPropertyName("Max")]
		public string Max { get; set; }

		[SerializationPropertyName("ImageURL")]
		public string ImageURL { get; set; }

		[SerializationPropertyName("Children")]
		public List<Child> Children { get; set; }

		[SerializationPropertyName("HardwareId")]
		public string HardwareId { get; set; }

		[SerializationPropertyName("SensorId")]
		public string SensorId { get; set; }

		[SerializationPropertyName("Type")]
		public string Type { get; set; }

		[SerializationPropertyName("RawMin")]
		public string RawMin { get; set; }

		[SerializationPropertyName("RawValue")]
		public string RawValue { get; set; }

		[SerializationPropertyName("RawMax")]
		public string RawMax { get; set; }
	}

	public class LibreHardwareData
	{
		[SerializationPropertyName("id")]
		public int Id { get; set; }

		[SerializationPropertyName("Text")]
		public string Text { get; set; }

		[SerializationPropertyName("Min")]
		public string Min { get; set; }

		[SerializationPropertyName("Value")]
		public string Value { get; set; }

		[SerializationPropertyName("Max")]
		public string Max { get; set; }

		[SerializationPropertyName("ImageURL")]
		public string ImageURL { get; set; }

		[SerializationPropertyName("Children")]
		public List<Child> Children { get; set; }
	}
}