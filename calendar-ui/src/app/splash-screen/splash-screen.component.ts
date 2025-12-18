import { Component, OnInit, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
    selector: 'app-splash-screen',
    standalone: true,
    imports: [CommonModule],
    templateUrl: './splash-screen.component.html',
    styleUrls: ['./splash-screen.component.css']
})
export class SplashScreenComponent implements OnInit {
    @Output() complete = new EventEmitter<void>();

    ngOnInit() {
        setTimeout(() => {
            this.complete.emit();
        }, 3000); // Matches optimized CSS animation duration
    }
}
