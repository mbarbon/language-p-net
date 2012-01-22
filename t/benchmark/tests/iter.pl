#!/usr/bin/perl -w

sub fib {
    my $n = $_[0];

    my $p0 = 0;
    my $p1 = 1;
    for( my $i = 0; $i < $n; $i = $i + 1 ) {
        print "$i\n";
        my $t = $p1 + $p0;
        $p0 = $p1;
        $p1 = $t;
    }

    return $p1;
}

fib( 1000000 );
