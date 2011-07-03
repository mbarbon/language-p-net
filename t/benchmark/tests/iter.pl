#!/usr/bin/perl -w

sub fib {
    my( $n ) = @_;

    my( $p0, $p1 ) = ( 0, 1 );
    for( my $i = 0; $i < $n; ++$i ) {
        my $t = $p1 + $p0;
        $p0 = $p1;
        $p1 = $t;
    }

    return $p1;
}

fib( 1000000 );
