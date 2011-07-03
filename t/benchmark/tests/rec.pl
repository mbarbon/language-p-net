#!/usr/bin/perl -w

sub fib {
    my( $n ) = @_;

    if( $n >= 2 ) {
        return fib( $n - 2 ) + fib( $n - 1 );
    } else {
        return 1;
    }
}

fib( 27 );

