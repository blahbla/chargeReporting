create table charge_tibber_price
(
    id       int auto_increment
        primary key,
    currency varchar(255) null,
    energy   float        null,
    tax      float        null,
    total    float        null,
    startsat datetime     null,
    constraint charge_tibber_price_id_uindex
        unique (id),
    constraint charge_tibber_price_startsat_uindex
        unique (startsat)
);