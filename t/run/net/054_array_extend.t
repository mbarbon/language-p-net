#!/usr/bin/perl -w

print "1..2\n";

if( defined &Internals::Net::get_class ) {
    {
        package Example::Class;

        Internals::Net::extend( 'Example::Class', 'System.DateTime' );
    }

    $date_class = Internals::Net::get_class( 'System.DateTime' );
    $array_class = Internals::Net::get_class( 'System.Array' );

    $array = $array_class->CreateInstance( $date_class, 3 );
    $array->[0] = Internals::Net::create( $date_class, 2010, 12, 16, 22, 13, 42 );
    $array->[1] = Example::Class->new( 2010, 12, 16, 22, 13, 42 );

    print $array->[0] eq '12/16/2010 22:13:42' ? "ok\n" : "not ok - $array->[0]\n";
    print $array->[1] eq '12/16/2010 22:13:42' ? "ok\n" : "not ok - $array->[1]\n";
} else {
    print "ok - skipped\n" for 1..2;
}
