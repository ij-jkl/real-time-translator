import { ComponentFixture, TestBed } from '@angular/core/testing';

import { LogPanel } from './log-panel';

describe('LogPanel', () => {
  let component: LogPanel;
  let fixture: ComponentFixture<LogPanel>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [LogPanel]
    })
    .compileComponents();

    fixture = TestBed.createComponent(LogPanel);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
