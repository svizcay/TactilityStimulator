using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Inria.Tactility.GUI
{
    public class StimulatorStatus : MonoBehaviour
    {
        private Text guiText;
        private Image led;

        private string promptStr = "stimulator status: ";

        private IEnumerator connectingCoroutine;

        private void Awake()
        {
            guiText = GetComponent<Text>();
            guiText.text = promptStr + "connecting";
            led = GetComponentInChildren<Image>();
            connectingCoroutine = ConnectingCoroutine();
            StartCoroutine(connectingCoroutine);
        }

        public void OnStimulatorCouldntConnect ()
        {
            if (connectingCoroutine != null) StopCoroutine(connectingCoroutine);
            guiText.text = promptStr + "couldn't connect";
            led.color = Color.gray;
        }

        public void OnStimulatorConnected ()
        {
            if (connectingCoroutine != null) StopCoroutine(connectingCoroutine);
            guiText.text = promptStr + "connected";
            led.color = Color.yellow;
        }

        public void OnStimStart()
        {
            guiText.text = promptStr + " ON";
            led.color = Color.green;
        }

        public void OnStimEnd()
        {
            guiText.text = promptStr + " OFF";
            led.color = Color.yellow;
        }

        IEnumerator ConnectingCoroutine ()
        {
            int dots = 0;
            while (true)
            {
                StringBuilder builder = new StringBuilder();
                builder.Append(promptStr);
                builder.Append("connecting ");
                for (int i = 0; i <= dots; i++)
                {
                    builder.Append(".  ");
                }

                guiText.text = builder.ToString();
                dots = (dots + 1) % 4;
                yield return new WaitForSeconds(0.2f);
            }

        }
    }

}

