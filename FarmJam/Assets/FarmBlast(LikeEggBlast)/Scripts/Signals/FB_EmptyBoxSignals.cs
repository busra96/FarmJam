using FarmBlast;
using UnityEngine;

namespace Signals
{
    public class FB_EmptyBoxSignals
    {
        public static Signal<FB_EmptyUnitBoxParentMovement> OnTheBoxHasCompletedTheMovementToTheStartingPosition = new Signal<FB_EmptyUnitBoxParentMovement>();
        public static Signal<FB_EmptyUnitBoxParent> OnTheEmptyBoxRemoved = new Signal<FB_EmptyUnitBoxParent>();
        public static Signal OnFailConditionCheck = new Signal();

        public static Signal<FB_EmptyUnitBoxParent> OnAddedEmptyBox = new Signal<FB_EmptyUnitBoxParent>();
        public static Signal<FB_EmptyUnitBoxParent> OnRemovedEmptyBox = new Signal<FB_EmptyUnitBoxParent>();
    }
}
