# Code adapted from: http://www.taylortree.com/2010/11/running-variance.html

def running_sma(bar, series, period, prevma):
        """
        Returns the running simple moving average - avoids sum of series per call.
     
        Keyword arguments:
        bar     --  current index or location of the value in the series
        series  --  list or tuple of data to average
        period  --  number of values to include in average
        prevma  --  previous simple moving average (n - 1) of the series
        """
     
        if period < 1:
            raise ValueError("period must be 1 or greater")
     
        if bar <= 0:
            return series[0]
     
        elif bar < period:
            return cumulative_sma(bar, series, prevma)
     
        return prevma + ((series[bar] - series[bar - period]) / float(period))

def cumulative_sma(bar, series, prevma):
        """
        Returns the cumulative or unweighted simple moving average.
        Avoids sum of series per call.
     
        Keyword arguments:
        bar     --  current index or location of the value in the series
        series  --  list or tuple of data to average
        prevma  --  previous average (n - 1) of the series.
        """
       
        if bar <= 0:
            return series[0]
     
        return prevma + ((series[bar] - prevma) / (bar + 1.0))

def powersumavg(bar, series, period, pval=None):
        """
        Returns the power sum average based on the blog post from
        Subliminal Messages.  Use the power sum average to help derive the running
        variance.
        sources: http://subluminal.wordpress.com/2008/07/31/running-standard-deviations/
     
        Keyword arguments:
        bar     --  current index or location of the value in the series
        series  --  list or tuple of data to average
        period  -- number of values to include in average
        pval    --  previous powersumavg (n - 1) of the series.
        """
     
        if period < 1:
            raise ValueError("period must be 1 or greater")
     
        if bar < 0:
            bar = 0
     
        if pval == None:
            if bar > 0:
                raise ValueError("pval of None invalid when bar > 0")
                
            pval = 0.0
        
        newamt = float(series[bar])
     
        if bar < period:
            result = pval + (newamt * newamt - pval) / (bar + 1.0)
     
        else:
            oldamt = float(series[bar - period])
            result = pval + (((newamt * newamt) - (oldamt * oldamt)) / period)
     
        return result
        
        
def running_var(bar, series, period, asma, apowsumavg):
    """
    Returns the running variance based on a given time period.
    sources: http://subluminal.wordpress.com/2008/07/31/running-standard-deviations/
 
    Keyword arguments:
    bar     --  current index or location of the value in the series
    series  --  list or tuple of data to average
    asma    --  current average of the given period
    apowsumavg -- current powersumavg of the given period
    """
    if period < 1:
        raise ValueError("period must be 1 or greater")
 
    if bar <= 0:
        return 0.0
 
    if asma == None:
        raise ValueError("asma of None invalid when bar > 0")
 
    if apowsumavg == None:
        raise ValueError("powsumavg of None invalid when bar > 0")
 
    windowsize = bar + 1.0
    if windowsize >= period:
        windowsize = period
 
    return (apowsumavg * windowsize - windowsize * asma * asma) / windowsize


list_of_values = [3.0, 5.0, 8.0, 10.0, 4.0, 5.0]
prev_powersumavg = None
prev_sma = None
prev_sma = None
period = 3
for bar, price in enumerate(list_of_values):
    new_sma = running_sma(bar, list_of_values, period, prev_sma)
    new_powersumavg = powersumavg(bar, list_of_values, period, prev_powersumavg)
    new_var = running_var(bar, list_of_values, period, new_sma, new_powersumavg)
 
    msg = "SMA=%.4f, PSA=%.4f, Var=%.4f" % (new_sma, new_powersumavg, new_var)
    print "bar %i: %s" % (bar, msg)
 
    prev_sma = new_sma
    prev_powersumavg = new_powersumavg
    
    



    