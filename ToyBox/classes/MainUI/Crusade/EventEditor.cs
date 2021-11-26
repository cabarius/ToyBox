using Kingmaker;
using ModKit;
using static ModKit.UI;
using Kingmaker.Kingdom;
using Kingmaker.Kingdom.Tasks;
using Kingmaker.Kingdom.Blueprints;
using Kingmaker.Blueprints;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ToyBox.classes.MainUI {
    public static class EventEditor {
        public static Settings settings => Main.settings;

        public static void OnGUI() {
            var ks = KingdomState.Instance;
            if (ks == null) {
                UI.Label("You must unlock the crusade before you can access these toys.".yellow().bold());
                return;
            }
            UI.Div(0, 25);
            UI.HStack("Events", 1,
                () => UI.Toggle("Preview Events", ref settings.previewEventResults),
                () => UI.Toggle("Instant Events", ref settings.toggleInstantEvent),
                () => UI.Toggle("Ignore Event Solution Restrictions", ref settings.toggleIgnoreEventSolutionRestrictions),
                () => {
                    using (UI.VerticalScope()) {
                        if (ks.ActiveEvents.Count == 0)
                            UI.Label("No active events".orange().bold());
                        foreach (var activeEvent in ks.ActiveEvents) {
                            /* If it's an event not a decree
                             * Events are associated with Tasks by EventTask
                             * EventTask is a child of Task
                             * Task(decree) must also have a corresponding event
                             * Event(AKA the "Event" in the game) does not have an associated task(EventTask)
                            */
                            if (activeEvent.AssociatedTask == null) {
                                Div(0, 25);
                                using (UI.HorizontalScope()) {
                                    UI.Label(activeEvent.FullName.cyan(), 350.width());
                                    25.space();
                                    UI.Label(activeEvent.EventBlueprint.InitialDescription.StripHTML().green());
                                }
                            }
                        }
                    }
                }
            );
            
             UI.Div(0, 25);
             UI.HStack("Decrees", 1,
                 () => UI.Toggle("Preview Decrees", ref settings.previewDecreeResults),
                 () => UI.Toggle("Ignore Start Restrictions", ref settings.toggleIgnoreStartTaskRestrictions, UI.AutoWidth()),
                //TODO: toggle to ignore specific restrictions
                () => UI.Toggle("No Decree Resource Costs", ref settings.toggleTaskNoResourcesCost),
                () => {
                     using (UI.VerticalScope()) {
                         
                         if (ks.ActiveTasks.Count() == 0)
                             UI.Label("No active decrees".orange().bold());
                         foreach (var activeTask in ks.ActiveEvents) {
                             if (activeTask.AssociatedTask != null) {
                                 Div(0,25);
                                 var task = activeTask.AssociatedTask;
                                 using (UI.HorizontalScope()) {
                                     UI.Label(task.Name.cyan(), 350.width());
                                     25.space();
                                     if (task.IsInProgress)
                                         UI.Label($"Ends in {task.EndsOn - ks.CurrentDay} days", 200.width());
                                     else {
                                         UI.ActionButton("Start", () => {
                                             task.Start();
                                         }, 200.width());
                                     }
                                     25.space();

                                     if (task.IsInProgress) {
                                         UI.ActionButton("Finish", () => {
                                             task.m_BonusDays = task.Duration;
                                         }, 120.width());
                                         if (task.CanCancelStarted) {
                                             UI.ActionButton("Cancel", () => {
                                                 task.Cancel();
                                             }, 120.width());
                                         }
                                         else
                                             123.space();
                                     }
                                     else
                                         249.space();
                                     25.space();
                                     var taskBlueprint = task.Event.EventBlueprint as BlueprintKingdomProject;
                                     UI.Label(task.Description.StripHTML().orange() + "\n" + taskBlueprint.MechanicalDescription.ToString().StripHTML().green());
                                 }
                             }
                         }
                         ks.TimelineManager.UpdateEvents();
                     }
                 }
             );

        }
    }
}
