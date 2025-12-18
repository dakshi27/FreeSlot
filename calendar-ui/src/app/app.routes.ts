import { Routes } from '@angular/router';
import { OverviewComponent } from './overview/overview.component';
import { CalendarComponent } from './calendar/calendar.component';
import { EventDetailsComponent } from './event-details/event-details.component';
import { RecommenderComponent } from './recommender/recommender.component';
import { ContributorsComponent } from './contributors/contributors.component';

export const routes: Routes = [
    { path: '', component: OverviewComponent },
    { path: 'calendar', component: CalendarComponent },
    { path: 'event-details', component: EventDetailsComponent },
    { path: 'recommender', component: RecommenderComponent },
    { path: 'contributors', component: ContributorsComponent },
];
