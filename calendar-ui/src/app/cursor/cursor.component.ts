import { Component, HostListener, signal, Input, booleanAttribute, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-cursor',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './cursor.component.html',
  styleUrl: './cursor.component.css'
})
export class CursorComponent {
  @Input({ transform: booleanAttribute }) isDarkMode = false;

  // Position
  x = signal(0);
  y = signal(0);

  // Appearance (Default state)
  width = signal(100);
  height = signal(100);
  borderRadius = signal('50%');

  isVisible = signal(false);
  isHovering = signal(false); // State to toggle specific glow styles

  // Target cache to prevent layout thrashing
  private currentTarget: HTMLElement | null = null;

  @HostListener('document:mousemove', ['$event'])
  onMouseMove(event: MouseEvent) {
    if (!this.isVisible()) {
      this.isVisible.set(true);
    }

    // Only update position from mouse if NOT hovering a button
    // If hovering, the position is locked to the button center (see onMouseOver)
    if (!this.isHovering()) {
      this.x.set(event.clientX);
      this.y.set(event.clientY);
    }
  }

  @HostListener('document:mouseover', ['$event'])
  onMouseOver(event: MouseEvent) {
    const target = event.target as HTMLElement;
    // Check for interactive elements
    const interactive = target.closest('button, a, [role="button"], .p-element') as HTMLElement;

    if (interactive) {
      this.isHovering.set(true);
      this.currentTarget = interactive;
      this.snapToElement(interactive);
    } else {
      // If we moved out of a button into a non-interactive area
      // But 'mouseover' fires for child elements too, so be careful.
      // We rely on checking closest() implementation.
      // If snap logic is purely on mouseover, we need to handle "mouseout" effectively 
      // or check if the new target is NOT part of the current target/interactive chain
    }
  }

  @HostListener('document:mouseout', ['$event'])
  onMouseOut(event: MouseEvent) {
    const target = event.target as HTMLElement;
    const related = event.relatedTarget as HTMLElement;

    // If we were hovering something, and we moussed out...
    if (this.currentTarget) {
      // If the new target is NOT inside the current cached target
      if (!this.currentTarget.contains(related)) {
        this.resetCursor();
      }
    } else {
      // Just leaving the window
      if (!related) {
        this.isVisible.set(false);
      }
    }
  }

  // Alternatively, just do everything in MouseMove for simplicity if performance allows?
  // No, checking bounding rect every move is heavy. event delegation is better.

  // Correction: mouseover bubbles. So moving inside the button fires it.
  // We need to ensure we don't re-snap unnecessarily if already snapped to that same ID.

  private snapToElement(element: HTMLElement) {
    const rect = element.getBoundingClientRect();
    const style = window.getComputedStyle(element);

    this.width.set(rect.width);
    this.height.set(rect.height);
    this.borderRadius.set(style.borderRadius);

    // Center of the button
    this.x.set(rect.left + rect.width / 2);
    this.y.set(rect.top + rect.height / 2);
  }

  private resetCursor() {
    this.isHovering.set(false);
    this.currentTarget = null;
    this.width.set(100);
    this.height.set(100);
    this.borderRadius.set('50%');
    // x and y will pick up on next mousemove
  }
}
