// Test setup for Angular with Vitest
import { getTestBed } from '@angular/core/testing';
import {
    BrowserTestingModule,
    platformBrowserTesting,
} from '@angular/platform-browser/testing';

// Configure the Angular testing environment
getTestBed().initTestEnvironment(
  BrowserTestingModule,
  platformBrowserTesting()
);
