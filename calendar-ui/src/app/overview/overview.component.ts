import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';

@Component({
    selector: 'app-overview',
    standalone: true,
    imports: [CommonModule, RouterModule],
    templateUrl: './overview.component.html',
    styleUrl: './overview.component.css'
})
export class OverviewComponent { }
