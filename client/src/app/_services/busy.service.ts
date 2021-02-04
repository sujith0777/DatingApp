import { Injectable } from '@angular/core';
import { NgxSpinnerService } from 'ngx-spinner';

@Injectable({
  providedIn: 'root'
})
export class BusyService {
  BusyRequestCount = 0;

  constructor(private spinnerService: NgxSpinnerService) { }

  busy(){
    this.BusyRequestCount++;
    this.spinnerService.show(undefined,{
      type: 'line-scale-party',
      bdColor: 'rgba(255,255,255,0)',
      color: '#333333'
    })
  }

  idle(){
    this.BusyRequestCount--;
    if(this.BusyRequestCount==0){
      this.BusyRequestCount = 0;
      this.spinnerService.hide();
    }
  }

}
