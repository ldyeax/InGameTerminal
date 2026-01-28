using UnityEngine;

namespace InGameTerminal
{
	public class TestHolder : MonoBehaviour
	{
		public GameObject Held;

		private void Awake()
		{

		}

		private void Start()
		{

		}

		public void Pudim()
		{
			Held.SetActive(true);
		}
	}
}
