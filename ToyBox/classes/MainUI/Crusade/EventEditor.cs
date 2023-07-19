using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Kingdom;
using Kingmaker.Kingdom.Blueprints;
using Kingmaker.Kingdom.Tasks;
using ModKit;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static ModKit.UI;

namespace ToyBox.classes.MainUI {
    public static class EventEditor {
        public static Settings settings => Main.Settings;

        public static void OnGUI() {
            if (Game.Instance?.Player == null) return;
            var ks = KingdomState.Instance;
            if (ks == null) {
                Label("You must unlock the crusade before you can access these toys.".localize().yellow().bold());
                return;
            }
            Div(0, 25);
            HStack("Events".localize(), 1,
                () => Toggle("Preview Events".localize(), ref settings.previewEventResults),
                () => Toggle("Instant Events".localize(), ref settings.toggleInstantEvent),
                () => {
                    using (VerticalScope()) {
                        Toggle("Ignore Event Solution Restrictions".localize(), ref settings.toggleIgnoreEventSolutionRestrictions);
                        if (settings.toggleIgnoreEventSolutionRestrictions) {
                            using (HorizontalScope()) {
                                50.space();
                                Toggle("Hide Even Solution Restrictions Preview".localize(), ref settings.toggleHideEventSolutionRestrictionsPreview);
                            }
                        }
                    }
                },
                () => {
                    using (VerticalScope()) {
                        if (ks.ActiveEvents?.Count == 0)
                            Label("No active events".localize().orange().bold());
                        foreach (var activeEvent in ks.ActiveEvents) {
                            /* If it's an event not a decree
                             * Events are associated with Tasks by EventTask
                             * EventTask is a child of Task
                             * Task(decree) must also have a corresponding event
                             * Event(AKA the "Event" in the game) does not have an associated task(EventTask)
                            */
                            if (activeEvent.AssociatedTask == null) {
                                Div(0, 25);
                                using (HorizontalScope()) {
                                    Label(activeEvent.FullName.cyan(), 350.width());
                                    25.space();
                                    Label(activeEvent.EventBlueprint.InitialDescription.StripHTML().green());
                                }
                            }
                        }
                    }
                }
            );

            Div(0, 25);
            HStack("Decrees".localize(), 1,
                () => Toggle("Preview Decrees".localize(), ref settings.previewDecreeResults),
                () => Toggle("Ignore Start Restrictions".localize(), ref settings.toggleIgnoreStartTaskRestrictions, AutoWidth()),
                //TODO: toggle to ignore specific restrictions
                () => Toggle("No Decree Resource Costs".localize(), ref settings.toggleTaskNoResourcesCost),
                () => {
                    using (VerticalScope()) {

                        if (ks.ActiveTasks.Count() == 0)
                            Label("No active decrees".localize().orange().bold());
                        foreach (var activeTask in ks.ActiveEvents) {
                            if (activeTask.AssociatedTask != null) {
                                Div(0, 25);
                                var task = activeTask.AssociatedTask;
                                using (HorizontalScope()) {
                                    Label(task.Name.cyan(), 350.width());
                                    25.space();
                                    if (task.IsInProgress)
                                        Label("Ends in".localize() + (task.EndsOn - ks.CurrentDay).ToString() + "days".localize(), 200.width());
                                    else {
                                        ActionButton("Start".localize(), () => {
                                            task.Start();
                                        }, 200.width());
                                    }
                                    25.space();

                                    if (task.IsInProgress) {
                                        ActionButton("Finish".localize(), () => {
                                            task.m_BonusDays = task.Duration;
                                        }, 120.width());
                                        if (task.CanCancelStarted) {
                                            ActionButton("Cancel".localize(), () => {
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
                                    Label(task.Description.StripHTML().orange() + "\n" + taskBlueprint.MechanicalDescription.ToString().StripHTML().green());
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
