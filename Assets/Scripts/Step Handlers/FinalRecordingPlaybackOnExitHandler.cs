using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FinalRecordingPlaybackOnExitHandler : DialogueStepHandler
{
    public override void OnStepExit(DialogueFlowController controller, DialogueFlowController.StepDefinition step)
    {
        controller.PlayAllRecordedVoicesInOrder();
    }
}
