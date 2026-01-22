using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace InGameTerminal.Test
{
	public class Spinner : MonoBehaviour
	{
		[Range(-10.0f, 10.0f)]
		public float x = 0;
		[Range(-10.0f, 10.0f)]
		public float y = 1;
		[Range(-10.0f, 10.0f)]
		public float z = 0;
		private void Update()
		{
			transform.Rotate(new Vector3(x, y, z) * Time.deltaTime);
		}
	}
}
