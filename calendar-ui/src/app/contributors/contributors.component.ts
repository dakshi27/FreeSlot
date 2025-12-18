import { Component, signal, AfterViewInit, ElementRef, ViewChildren, QueryList, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
    selector: 'app-contributors',
    standalone: true,
    imports: [CommonModule],
    templateUrl: './contributors.component.html',
    styleUrl: './contributors.component.css'
})
export class ContributorsComponent implements AfterViewInit, OnDestroy {

    @ViewChildren('section') sections!: QueryList<ElementRef>;
    private observer!: IntersectionObserver;

    team = signal([
        {
            name: 'Dakshitha',
            role: 'Full Stack Lead',
            theme: 'lead',
            description: 'Driving the technical vision and ensuring seamless integration across all modules. Architecting the future of enterprise scheduling.',
            image: 'assets/team/dakshitha.jpg',
            avatarSeed: 'Dakshitha',
            contributions: ['Algorithm Design', 'UI/UX Strategy', 'Code Quality']
        },
        {
            name: 'Dhanushree',
            role: 'Backend Lead',
            theme: 'backend',
            description: 'Building the robust core API and implementing secure OAuth 2.0 authentication. Ensuring data security and seamless integration with external services.',
            image: 'assets/team/dhanu.jpg',
            avatarSeed: 'Dhanushree',
            contributions: ['API Design', 'OAuth 2.0', 'Security']
        },
        {
            name: 'Veena',
            role: 'Frontend Lead',
            theme: 'frontend',
            description: 'Creating intuitive user flows and responsive interfaces for optimal experience. championing accessibility and fluid interactions.',
            image: 'assets/team/veena.jpg',
            avatarSeed: 'Veena',
            contributions: ['Component Library', 'Responsive Design', 'Accessibility']
        },
        {
            name: 'Bhavani',
            role: 'Frontend Lead',
            theme: 'frontend',
            description: 'Ensuring pixel-perfect implementation and consistent design system usage. Bringing designs to life with precision code.',
            image: 'assets/team/bhavani.jpg',
            avatarSeed: 'Bhavani',
            contributions: ['Design System', 'Themes', 'Performance'],
            objectPosition: 'center 20%'
        }
    ]);

    constructor(private el: ElementRef) { }

    ngAfterViewInit() {
        // Set up the Intersection Observer
        const options = {
            root: null, // Use the viewport
            rootMargin: '-20% 0px', // Trigger when 20% into the viewport
            threshold: 0.3
        };

        this.observer = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    entry.target.classList.add('in-view');
                } else {
                    // Optional item: remove class to re-trigger animation on scroll up?
                    // For pure locomotive feel, usually we keep it or re-trigger. 
                    // Let's keep it added for now, or remove if we want re-reveal.
                    entry.target.classList.remove('in-view');
                }
            });
        }, options);

        // Target all sections
        const domSections = this.el.nativeElement.querySelectorAll('.snap-section');
        domSections.forEach((section: any) => {
            this.observer.observe(section);
        });
    }

    ngOnDestroy() {
        if (this.observer) {
            this.observer.disconnect();
        }
    }

}
