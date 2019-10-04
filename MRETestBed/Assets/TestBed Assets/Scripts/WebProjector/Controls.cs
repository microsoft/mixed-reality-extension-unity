using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace AltspaceVR.WebProjector
{
	public class Controls : MonoBehaviour
	{
		public WebProjector WebProjector;
		public UnityEngine.UI.Button PowerButton;

		private bool poweredOn;

		private void Awake()
		{
			UpdatePowerButton();
		}

		public void PowerButtonClicked()
		{
			poweredOn = !poweredOn;
			UpdatePowerButton();
			if (poweredOn)
			{
				WebProjector.RPC.StartProjectingMyRoom();
			}
			else
			{
				WebProjector.RPC.StopProjectingMyRoom();
			}
		}

		public void SetPowerButton(bool state)
		{
			poweredOn = state;
			UpdatePowerButton();
		}

		private void UpdatePowerButton()
		{
			if (!poweredOn)
			{
				PowerButton.image.color = new Color(0.5f, 0.5f, 0.5f);
			}
			else
			{
				PowerButton.image.color = new Color(0f, 1f, 0.1f);
			}
		}
	}
}
