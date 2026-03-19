import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { APP_VERSION, APP_BUILD_DATE } from '../../config/app-version';

@Component({
  selector: 'app-footer',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './footer.component.html',
  styleUrls: ['./footer.component.css']
})
export class FooterComponent {
  readonly appVersion = APP_VERSION;
  readonly buildDate = APP_BUILD_DATE;
  readonly currentYear = new Date().getFullYear();
}
